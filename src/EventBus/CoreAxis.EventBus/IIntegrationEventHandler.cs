using System.Threading.Tasks;

namespace CoreAxis.EventBus
{
    /// <summary>
    /// Interface for integration event handlers.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of the integration event.</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IntegrationEvent
    {
        /// <summary>
        /// Handles an integration event.
        /// </summary>
        /// <param name="event">The integration event to handle.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(TIntegrationEvent @event);
    }

    /// <summary>
    /// Marker interface for integration event handlers.
    /// </summary>
    public interface IIntegrationEventHandler
    {
    }

    /// <summary>
    /// Interface for dynamic integration event handlers.
    /// </summary>
    public interface IDynamicIntegrationEventHandler : IIntegrationEventHandler
    {
        /// <summary>
        /// Handles a dynamic integration event.
        /// </summary>
        /// <param name="eventData">The event data as a string.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(dynamic eventData, string eventName);
    }
}