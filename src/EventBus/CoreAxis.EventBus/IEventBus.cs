using CoreAxis.SharedKernel.IntegrationEvents;
using System;
using System.Threading.Tasks;

namespace CoreAxis.EventBus
{
    /// <summary>
    /// Interface for the event bus that handles publishing and subscribing to events.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an integration event to the event bus.
        /// </summary>
        /// <typeparam name="TIntegrationEvent">The type of the integration event.</typeparam>
        /// <param name="event">The integration event to publish.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event) where TIntegrationEvent : CoreAxis.SharedKernel.IntegrationEvents.IntegrationEvent;
        
        /// <summary>
        /// Publishes a dynamic integration event to the event bus.
        /// </summary>
        /// <param name="event">The integration event to publish.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishDynamicAsync(object @event, string eventName);

        /// <summary>
        /// Subscribes to an integration event.
        /// </summary>
        /// <typeparam name="TIntegrationEvent">The type of the integration event.</typeparam>
        /// <typeparam name="THandler">The type of the handler.</typeparam>
        void Subscribe<TIntegrationEvent, THandler>()
            where TIntegrationEvent : CoreAxis.SharedKernel.IntegrationEvents.IntegrationEvent
            where THandler : IIntegrationEventHandler<TIntegrationEvent>;
            
        /// <summary>
        /// Subscribes to an integration event with a specific handler instance.
        /// </summary>
        /// <typeparam name="TIntegrationEvent">The type of the integration event.</typeparam>
        /// <param name="handler">The integration event handler instance.</param>
        void Subscribe<TIntegrationEvent>(IIntegrationEventHandler<TIntegrationEvent> handler)
            where TIntegrationEvent : CoreAxis.SharedKernel.IntegrationEvents.IntegrationEvent;

        /// <summary>
        /// Unsubscribes from an integration event.
        /// </summary>
        /// <typeparam name="TIntegrationEvent">The type of the integration event.</typeparam>
        /// <typeparam name="THandler">The type of the handler.</typeparam>
        void Unsubscribe<TIntegrationEvent, THandler>()
            where TIntegrationEvent : CoreAxis.SharedKernel.IntegrationEvents.IntegrationEvent
            where THandler : IIntegrationEventHandler<TIntegrationEvent>;
            
        /// <summary>
        /// Unsubscribes from an integration event with a specific handler instance.
        /// </summary>
        /// <typeparam name="TIntegrationEvent">The type of the integration event.</typeparam>
        /// <param name="handler">The integration event handler instance.</param>
        void Unsubscribe<TIntegrationEvent>(IIntegrationEventHandler<TIntegrationEvent> handler)
            where TIntegrationEvent : CoreAxis.SharedKernel.IntegrationEvents.IntegrationEvent;

        /// <summary>
        /// Subscribes to an integration event dynamically.
        /// </summary>
        /// <param name="eventType">The type of the integration event.</param>
        /// <param name="handler">The dynamic handler.</param>
        void SubscribeDynamic<THandler>(string eventName, THandler handler)
            where THandler : IDynamicIntegrationEventHandler;

        /// <summary>
        /// Unsubscribes from an integration event dynamically.
        /// </summary>
        /// <param name="eventType">The type of the integration event.</param>
        /// <param name="handler">The dynamic handler.</param>
        void UnsubscribeDynamic<THandler>(string eventName, THandler handler)
            where THandler : IDynamicIntegrationEventHandler;
    }
}