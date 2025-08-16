using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers
{
    /// <summary>
    /// Health check controller for API Manager module.
    /// </summary>
    [ApiController]
    [Route("api/apimanager/[controller]")]
    public class HealthController : ControllerBase
    {
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
    }
}