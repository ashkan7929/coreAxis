using System;

namespace CoreAxis.SharedKernel.IntegrationEvents
{
    /// <summary>
    /// Base class for all integration events in the system.
    /// Integration events are used for communication between different modules or bounded contexts.
    /// </summary>
    public abstract class IntegrationEvent
    {
        /// <summary>
        /// Gets the unique identifier for this event instance.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this event was created.
        /// </summary>
        public DateTime CreationDate { get; }

        /// <summary>
        /// Gets the type name of the event.
        /// </summary>
        public string EventType => GetType().Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationEvent"/> class.
        /// </summary>
        protected IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationEvent"/> class with the specified ID and creation date.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="creationDate">The event creation date.</param>
        protected IntegrationEvent(Guid id, DateTime creationDate)
        {
            Id = id;
            CreationDate = creationDate;
        }
    }
}