using CoreAxis.EventBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace CoreAxis.SharedKernel.Observability.HealthChecks;

public class EventBusHealthCheck : IHealthCheck
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventBusHealthCheck> _logger;

    public EventBusHealthCheck(IEventBus eventBus, ILogger<EventBusHealthCheck> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // For in-memory bus, resolvability is sufficient. For production transports, implement a lightweight ping.
        if (_eventBus is null)
        {
            _logger.LogWarning("IEventBus not resolved in container.");
            return Task.FromResult(HealthCheckResult.Unhealthy("EventBus not available"));
        }

        return Task.FromResult(HealthCheckResult.Healthy("EventBus resolved"));
    }
}