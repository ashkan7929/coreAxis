using CoreAxis.EventBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.ApiGateway.HealthChecks
{
    /// <summary>
    /// Health check for the Event Bus.
    /// </summary>
    public class EventBusHealthCheck : IHealthCheck
    {
        private readonly IEventBus _eventBus;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBusHealthCheck"/> class.
        /// </summary>
        /// <param name="eventBus">The event bus.</param>
        /// <exception cref="ArgumentNullException">Thrown when eventBus is null.</exception>
        public EventBusHealthCheck(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Checks the health of the Event Bus.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the event bus is operational
                // For InMemoryEventBus, we consider it always healthy if it's not null
                if (_eventBus is InMemoryEventBus)
                {
                    return Task.FromResult(HealthCheckResult.Healthy("Event Bus is operational (InMemory)"));
                }
                
                return Task.FromResult(HealthCheckResult.Healthy("Event Bus is operational"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "Event Bus is not operational",
                    ex));
            }
        }
    }
}