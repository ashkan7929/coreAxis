using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Linq;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers
{
    /// <summary>
    /// Health check controller for API Manager module.
    /// </summary>
    [ApiController]
    [Route("api/apimanager/[controller]")]
    public class HealthController : ControllerBase
    {
        private sealed class ProbeResult
        {
            public string Service { get; init; } = string.Empty;
            public string Url { get; init; } = string.Empty;
            public bool Ok { get; init; }
            public long LatencyMs { get; init; }
            public string? Error { get; init; }
        }

        private sealed class ReadinessStatus
        {
            public bool Db { get; set; }
            public bool HttpClient { get; set; }
            public List<ProbeResult> Probes { get; } = new List<ProbeResult>();
            public int TotalProbes => Probes.Count;
            public int SucceededProbes => Probes.Count(p => p.Ok);
            public int FailedProbes => Probes.Count(p => !p.Ok);
            public double AvgLatencyMs => Probes.Count == 0 ? 0 : Probes.Average(p => (double)p.LatencyMs);
        }

        private readonly ApiManagerDbContext _dbContext;
        private readonly HealthCheckService _healthCheckService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="healthCheckService">The health check service.</param>
        public HealthController(ApiManagerDbContext dbContext, HealthCheckService healthCheckService)
        {
            _dbContext = dbContext;
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Gets the health status of the API Manager module.
        /// </summary>
        /// <returns>The health status.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration,
                Entries = healthReport.Entries
            };

            return healthReport.Status == HealthStatus.Healthy 
                ? Ok(response) 
                : StatusCode(503, response);
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        /// <returns>The connection test result.</returns>
    [HttpGet("test-db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    var webServiceCount = await _dbContext.WebServices.CountAsync();
                    return Ok(new { 
                        Status = "Healthy", 
                        Message = "Database connection successful",
                        WebServicesCount = webServiceCount
                    });
                }
                
                return StatusCode(503, new { 
                    Status = "Unhealthy", 
                    Message = "Database connection failed" 
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(503, new { 
                    Status = "Unhealthy", 
                    Message = "Database connection error",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Readiness endpoint verifying basic dependencies and optional synthetic probes.
        /// </summary>
        /// <param name="withProbes">Run cheap HEAD/GET probes against a few services.</param>
        /// <param name="maxProbeCount">Max number of services to probe.</param>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready([FromQuery] bool withProbes = false, [FromQuery] int maxProbeCount = 3)
        {
            var readiness = new ReadinessStatus();

            // Database connectivity
            try
            {
                readiness.Db = await _dbContext.Database.CanConnectAsync();
            }
            catch { readiness.Db = false; }

            // HttpClient factory sanity
            try
            {
                var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient();
                readiness.HttpClient = client != null;
            }
            catch { readiness.HttpClient = false; }

            // Optional synthetic probes with tight timeout budget
            if (withProbes)
            {
                var services = await _dbContext.WebServices.Where(ws => ws.IsActive).Take(Math.Max(0, maxProbeCount)).ToListAsync();
                foreach (var svc in services)
                {
                    var probeResult = new ProbeResult { Service = svc.Name, Url = svc.BaseUrl, Ok = false, LatencyMs = 0L, Error = null };
                    try
                    {
                        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var client = factory.CreateClient();
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, svc.BaseUrl), cts.Token);
                        sw.Stop();
                        probeResult = new ProbeResult { Service = svc.Name, Url = svc.BaseUrl, Ok = resp.IsSuccessStatusCode, LatencyMs = sw.ElapsedMilliseconds, Error = null };
                    }
                    catch (Exception ex)
                    {
                        probeResult = new ProbeResult { Service = svc.Name, Url = svc.BaseUrl, Ok = false, LatencyMs = 0L, Error = ex.Message };
                    }
                    readiness.Probes.Add(probeResult);
                }
            }

            var isReady = readiness.Db && readiness.HttpClient;
            return isReady ? Ok(readiness) : StatusCode(503, readiness);
        }
    }
}