using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CoreAxis.Modules.ApiManager.Application.HealthChecks;

namespace CoreAxis.Modules.ApiManager.Application.HealthChecks
{
    /// <summary>
    /// Extensions for configuring health checks in the API Manager module.
    /// </summary>
    public static class HealthChecksExtensions
    {
        /// <summary>
        /// Adds API Manager health checks to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddApiManagerHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<ApiManagerHealthCheck>(
                    "api_manager_health_check",
                    HealthStatus.Unhealthy,
                    new[] { "api_manager", "database" });

            return services;
        }
    }
}