using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ApiManager.Application.HealthChecks
{
    /// <summary>
    /// Health check for the API Manager module.
    /// </summary>
    public class ApiManagerHealthCheck : IHealthCheck
    {
        private readonly ApiManagerDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiManagerHealthCheck"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public ApiManagerHealthCheck(ApiManagerDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Checks the health of the API Manager module.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check database connectivity
                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                
                if (!canConnect)
                {
                    return new HealthCheckResult(
                        context.Registration.FailureStatus,
                        "API Manager database connection failed");
                }

                // Check if we can query the database
                var webServiceCount = await _dbContext.WebServices.CountAsync(cancellationToken);
                
                return HealthCheckResult.Healthy($"API Manager is healthy. WebServices count: {webServiceCount}");
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "API Manager is unhealthy",
                    ex);
            }
        }
    }
}