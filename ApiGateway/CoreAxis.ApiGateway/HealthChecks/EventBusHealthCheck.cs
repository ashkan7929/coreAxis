using CoreAxis.EventBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.ApiGateway.HealthChecks
{
    /// <summary>
    /// Health check for the event bus.
    /// </summary>
    public class EventBusHealthCheck : IHealthCheck
    {
        private readonly IEventBus _eventBus;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBusHealthCheck"/> class.
        /// </summary>
        /// <param name="eventBus">The event bus.</param>
        public EventBusHealthCheck(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Checks the health of the event bus.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // For InMemoryEventBus, we can just check if it's not null
                // For a real message broker like RabbitMQ, we would check the connection status
                if (_eventBus is InMemoryEventBus)
                {
                    return Task.FromResult(HealthCheckResult.Healthy("Event bus is operational"));
                }
                
                // For other implementations, we would need to check their specific health indicators
                // This is a placeholder for future implementations
                return Task.FromResult(HealthCheckResult.Healthy("Event bus is operational"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "Event bus is not operational",
                    ex));
            }
        }
    }
}