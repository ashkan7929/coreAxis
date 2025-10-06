using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreAxis.SharedKernel.Observability.HealthChecks;

/// <summary>
/// Extensions to register shared health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers CoreAxis shared health checks.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="includeOutbox">Include Outbox liveness check (requires IOutboxRepository in DI).</param>
    /// <returns>The health checks builder for further configuration.</returns>
    public static IHealthChecksBuilder AddCoreAxisHealthChecks(this IServiceCollection services, bool includeOutbox = true)
    {
        var builder = services.AddHealthChecks();
        builder.AddCheck<EventBusHealthCheck>("eventbus");
        if (includeOutbox)
        {
            builder.AddCheck<OutboxLivenessHealthCheck>("outbox");
        }
        return builder;
    }
}