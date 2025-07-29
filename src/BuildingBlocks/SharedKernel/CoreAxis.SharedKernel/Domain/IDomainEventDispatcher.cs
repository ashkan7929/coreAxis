using CoreAxis.SharedKernel.DomainEvents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel.Domain
{
    /// <summary>
    /// Interface for dispatching domain events within a module.
    /// Domain events are used for intra-module communication and are handled synchronously.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// Dispatches a domain event to all registered handlers.
        /// </summary>
        /// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
        /// <param name="domainEvent">The domain event to dispatch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent;

        /// <summary>
        /// Dispatches multiple domain events to all registered handlers.
        /// </summary>
        /// <param name="domainEvents">The collection of domain events to dispatch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DispatchAsync(IEnumerable<DomainEvent> domainEvents);
    }
}