using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreAxis.ApiGateway.HealthChecks
{
    /// <summary>
    /// Extensions for configuring health checks in the API Gateway.
    /// </summary>
    public static class HealthChecksExtensions
    {
        /// <summary>
        /// Adds CoreAxis health checks to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddCoreAxisHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<ApiGatewayHealthCheck>("api_gateway_health_check");

            services.AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(5); // Sets the time interval in which HealthChecks will be triggered
                options.MaximumHistoryEntriesPerEndpoint(10); // Sets the maximum history entries per endpoint
                options.AddHealthCheckEndpoint("CoreAxis API", "/health"); // Registers the API endpoint
            })
            .AddInMemoryStorage(); // Adds the storage provider for the health check UI

            return services;
        }

        /// <summary>
        /// Uses CoreAxis health checks in the application.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseCoreAxisHealthChecks(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.UseHealthChecksUI(options =>
            {
                options.UIPath = "/health-ui";
                options.ApiPath = "/health-api";
            });

            return app;
        }
    }
}