using System;

namespace CoreAxis.SharedKernel.DomainEvents
{
    /// <summary>
    /// Base class for all domain events in the system.
    /// Domain events represent something that happened in the domain that domain experts care about.
    /// </summary>
    public abstract class DomainEvent
    {
        /// <summary>
        /// Gets the unique identifier for this event instance.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this event occurred.
        /// </summary>
        public DateTime OccurredOn { get; }

        /// <summary>
        /// Gets the type name of the event.
        /// </summary>
        public string EventType => GetType().Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent"/> class.
        /// </summary>
        protected DomainEvent()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
        }
    }
}