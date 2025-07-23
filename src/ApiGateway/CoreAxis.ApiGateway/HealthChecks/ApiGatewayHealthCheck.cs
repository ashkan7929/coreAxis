using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.ApiGateway.HealthChecks
{
    /// <summary>
    /// Health check for the API Gateway.
    /// </summary>
    public class ApiGatewayHealthCheck : IHealthCheck
    {
        /// <summary>
        /// Checks the health of the API Gateway.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Basic check to ensure the API Gateway is running
                // In a real-world scenario, you might want to check more complex dependencies
                return Task.FromResult(HealthCheckResult.Healthy("API Gateway is healthy"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "API Gateway is unhealthy",
                    ex));
            }
        }
    }
}