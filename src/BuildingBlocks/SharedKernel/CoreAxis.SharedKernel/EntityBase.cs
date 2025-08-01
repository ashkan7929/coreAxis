using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.Collections.Generic;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Base class for all entities in the system.
    /// Includes audit fields and domain event handling capabilities.
    /// </summary>
    public abstract class EntityBase
    {
        private readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();

        /// <summary>
        /// Gets the unique identifier for this entity.
        /// </summary>
        public Guid Id { get; protected set; }



        /// <summary>
        /// Gets or sets the identifier of the user who created this entity.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this entity was created.
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified this entity.
        /// </summary>
        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this entity was last modified.
        /// </summary>
        public DateTime? LastModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entity is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets the domain events raised by this entity.
        /// </summary>
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Adds a domain event to this entity.
        /// </summary>
        /// <param name="domainEvent">The domain event to add.</param>
        protected void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all domain events from this entity.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBase"/> class.
        /// </summary>
        protected EntityBase()
        {
            Id = Guid.NewGuid();
            CreatedOn = DateTime.UtcNow;
        }
    }
}