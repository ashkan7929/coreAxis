using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAxis.EventBus
{
    /// <summary>
    /// In-memory implementation of the event bus.
    /// </summary>
    public class InMemoryEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly Dictionary<string, List<Type>> _eventHandlerTypes;
        private readonly Dictionary<string, List<IDynamicIntegrationEventHandler>> _dynamicHandlers;
        private readonly Dictionary<string, List<object>> _handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventBus"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="logger">The logger.</param>
        public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventHandlerTypes = new Dictionary<string, List<Type>>();
            _dynamicHandlers = new Dictionary<string, List<IDynamicIntegrationEventHandler>>();
            _handlers = new Dictionary<string, List<object>>();
            _handlers = new Dictionary<string, List<object>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventBus"/> class.
        /// This constructor is primarily for testing purposes.
        /// </summary>
        public InMemoryEventBus()
        {
            _serviceProvider = null;
            _logger = null;
            _eventHandlerTypes = new Dictionary<string, List<Type>>();
            _dynamicHandlers = new Dictionary<string, List<IDynamicIntegrationEventHandler>>();
            _handlers = new Dictionary<string, List<object>>();
        }

        /// <inheritdoc/>
        public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : IntegrationEvent
        {
            var eventName = @event.GetType().Name;
            _logger?.LogInformation("Publishing event: {EventName}", eventName);

            // Process handlers registered for this event type via DI
            if (_eventHandlerTypes.ContainsKey(eventName) && _serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                foreach (var handlerType in _eventHandlerTypes[eventName])
                {
                    var handler = scope.ServiceProvider.GetService(handlerType);
                    if (handler == null)
                    {
                        _logger?.LogWarning("Handler {HandlerType} not registered in DI container", handlerType.Name);
                        continue;
                    }

                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
                    var handleMethod = concreteType.GetMethod("HandleAsync");
                    if (handleMethod != null)
                    {
                        try
                        {
                            await (Task)handleMethod.Invoke(handler, new object[] { @event });
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error handling event {EventName} by {HandlerType}", eventName, handlerType.Name);
                        }
                    }
                }
            }

            // Process directly registered handler instances
            if (_handlers.ContainsKey(eventName))
            {
                foreach (var handler in _handlers[eventName])
                {
                    try
                    {
                        // Get the generic interface type for this event
                        var handlerType = handler.GetType();
                        var eventType = @event.GetType();
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        
                        // Check if the handler implements the interface for this event type
                        if (concreteType.IsInstanceOfType(handler))
                        {
                            var handleMethod = concreteType.GetMethod("HandleAsync");
                            await (Task)handleMethod.Invoke(handler, new object[] { @event });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error handling event {EventName} by instance handler", eventName);
                    }
                }
            }

            // Process dynamic handlers
            if (_dynamicHandlers.ContainsKey(eventName))
            {
                foreach (var handler in _dynamicHandlers[eventName])
                {
                    try
                    {
                        await handler.HandleAsync(@event, eventName);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error handling dynamic event {EventName}", eventName);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Subscribe<TIntegrationEvent, THandler>()
            where TIntegrationEvent : IntegrationEvent
            where THandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            var eventName = typeof(TIntegrationEvent).Name;
            var handlerType = typeof(THandler);

            if (!_eventHandlerTypes.ContainsKey(eventName))
            {
                _eventHandlerTypes[eventName] = new List<Type>();
            }

            if (_eventHandlerTypes[eventName].Contains(handlerType))
            {
                _logger?.LogWarning("Handler {HandlerType} already registered for event {EventName}", handlerType.Name, eventName);
                return;
            }

            _eventHandlerTypes[eventName].Add(handlerType);
            _logger?.LogInformation("Subscribed handler {HandlerType} to event {EventName}", handlerType.Name, eventName);
        }

        /// <inheritdoc/>
        public void Subscribe<TIntegrationEvent>(IIntegrationEventHandler<TIntegrationEvent> handler)
            where TIntegrationEvent : IntegrationEvent
        {
            var eventName = typeof(TIntegrationEvent).Name;
            var handlerType = handler.GetType();

            if (!_eventHandlerTypes.ContainsKey(eventName))
            {
                _eventHandlerTypes[eventName] = new List<Type>();
            }

            // For instance-based subscription, we don't check for duplicates
            // as we might want to register the same handler type multiple times with different instances

            _eventHandlerTypes[eventName].Add(handlerType);
            _logger?.LogInformation("Subscribed handler instance {HandlerType} to event {EventName}", handlerType.Name, eventName);

            // Store the handler instance for later use
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers[eventName] = new List<object>();
            }

            _handlers[eventName].Add(handler);
        }

        /// <inheritdoc/>
        public void Unsubscribe<TIntegrationEvent, THandler>()
            where TIntegrationEvent : IntegrationEvent
            where THandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            var eventName = typeof(TIntegrationEvent).Name;
            var handlerType = typeof(THandler);

            if (!_eventHandlerTypes.ContainsKey(eventName))
            {
                return;
            }

            _eventHandlerTypes[eventName].Remove(handlerType);

            if (!_eventHandlerTypes[eventName].Any())
            {
                _eventHandlerTypes.Remove(eventName);
            }

            _logger?.LogInformation("Unsubscribed handler {HandlerType} from event {EventName}", handlerType.Name, eventName);
        }

        /// <inheritdoc/>
        public void Unsubscribe<TIntegrationEvent>(IIntegrationEventHandler<TIntegrationEvent> handler)
            where TIntegrationEvent : IntegrationEvent
        {
            var eventName = typeof(TIntegrationEvent).Name;

            if (!_handlers.ContainsKey(eventName))
            {
                return;
            }

            // Remove the handler instance
            _handlers[eventName].Remove(handler);

            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
            }

            _logger?.LogInformation("Unsubscribed handler instance from event {EventName}", eventName);
        }

        /// <inheritdoc/>
        public void SubscribeDynamic<THandler>(string eventName, THandler handler)
            where THandler : IDynamicIntegrationEventHandler
        {
            if (!_dynamicHandlers.ContainsKey(eventName))
            {
                _dynamicHandlers[eventName] = new List<IDynamicIntegrationEventHandler>();
            }

            if (_dynamicHandlers[eventName].Contains(handler))
            {
                _logger?.LogWarning("Dynamic handler already registered for event {EventName}", eventName);
                return;
            }

            _dynamicHandlers[eventName].Add(handler);
            _logger?.LogInformation("Subscribed dynamic handler to event {EventName}", eventName);
        }

        /// <inheritdoc/>
        public void UnsubscribeDynamic<THandler>(string eventName, THandler handler)
            where THandler : IDynamicIntegrationEventHandler
        {
            if (!_dynamicHandlers.ContainsKey(eventName))
            {
                return;
            }

            _dynamicHandlers[eventName].Remove(handler);

            if (!_dynamicHandlers[eventName].Any())
            {
                _dynamicHandlers.Remove(eventName);
            }

            _logger?.LogInformation("Unsubscribed dynamic handler from event {EventName}", eventName);
        }

        /// <inheritdoc/>
        public async Task PublishDynamicAsync(object @event, string eventName)
        {
            _logger?.LogInformation("Publishing dynamic event: {EventName}", eventName);

            // Process dynamic handlers
            if (_dynamicHandlers.ContainsKey(eventName))
            {
                foreach (var handler in _dynamicHandlers[eventName])
                {
                    try
                    {
                        await handler.HandleAsync(@event, eventName);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error handling dynamic event {EventName}", eventName);
                    }
                }
            }
        }
    }
}