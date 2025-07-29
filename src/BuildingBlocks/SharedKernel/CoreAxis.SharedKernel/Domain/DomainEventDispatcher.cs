using CoreAxis.SharedKernel.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel.Domain
{
    /// <summary>
    /// Default implementation of IDomainEventDispatcher that uses dependency injection
    /// to resolve and execute domain event handlers.
    /// </summary>
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Dispatches a domain event to all registered handlers.
        /// </summary>
        /// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
        /// <param name="domainEvent">The domain event to dispatch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent
        {
            if (domainEvent == null)
            {
                _logger.LogWarning("Attempted to dispatch null domain event");
                return;
            }

            _logger.LogDebug("Dispatching domain event {EventType} with ID {EventId}", 
                domainEvent.EventType, domainEvent.Id);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(typeof(TDomainEvent));
            var handlers = _serviceProvider.GetServices(handlerType);

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                try
                {
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent });
                        if (task != null)
                        {
                            tasks.Add(task);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while dispatching domain event {EventType} to handler {HandlerType}", 
                        domainEvent.EventType, handler.GetType().Name);
                    throw;
                }
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            _logger.LogDebug("Successfully dispatched domain event {EventType} to {HandlerCount} handlers", 
                domainEvent.EventType, tasks.Count);
        }

        /// <summary>
        /// Dispatches multiple domain events to all registered handlers.
        /// </summary>
        /// <param name="domainEvents">The collection of domain events to dispatch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents)
        {
            if (domainEvents == null || !domainEvents.Any())
            {
                _logger.LogDebug("No domain events to dispatch");
                return;
            }

            var tasks = new List<Task>();
            foreach (var domainEvent in domainEvents)
            {
                var dispatchMethod = typeof(DomainEventDispatcher)
                    .GetMethod(nameof(DispatchAsync), new[] { domainEvent.GetType() });
                
                if (dispatchMethod != null)
                {
                    var task = (Task)dispatchMethod.Invoke(this, new object[] { domainEvent });
                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }
    }

    /// <summary>
    /// Interface for domain event handlers.
    /// </summary>
    /// <typeparam name="TDomainEvent">The type of domain event to handle.</typeparam>
    public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : DomainEvent
    {
        /// <summary>
        /// Handles the specified domain event.
        /// </summary>
        /// <param name="domainEvent">The domain event to handle.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TDomainEvent domainEvent);
    }
}