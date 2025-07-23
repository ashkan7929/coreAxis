using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DemoModule.Domain
{
    /// <summary>
    /// Represents a demo item in the DemoModule.
    /// </summary>
    public class DemoItem : EntityBase
    {
        /// <summary>
        /// Gets or sets the name of the demo item.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the demo item.
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the price of the demo item.
        /// </summary>
        public decimal Price { get; private set; }

        /// <summary>
        /// Gets or sets the category of the demo item.
        /// </summary>
        public string Category { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the demo item is featured.
        /// </summary>
        public bool IsFeatured { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private DemoItem()
        {
        }

        /// <summary>
        /// Creates a new demo item.
        /// </summary>
        /// <param name="name">The name of the demo item.</param>
        /// <param name="description">The description of the demo item.</param>
        /// <param name="price">The price of the demo item.</param>
        /// <param name="category">The category of the demo item.</param>
        /// <returns>A new demo item.</returns>
        public static DemoItem Create(string name, string description, decimal price, string category)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            var demoItem = new DemoItem
            {
                Name = name,
                Description = description,
                Price = price,
                Category = category,
                IsFeatured = false
            };

            demoItem.AddDomainEvent(new DemoItemCreatedEvent(demoItem.Id, name));

            return demoItem;
        }

        /// <summary>
        /// Updates the demo item.
        /// </summary>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="price">The new price.</param>
        /// <param name="category">The new category.</param>
        public void Update(string name, string description, decimal price, string category)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            Name = name;
            Description = description;
            Price = price;
            Category = category;

            AddDomainEvent(new DemoItemUpdatedEvent(Id, name));
        }

        /// <summary>
        /// Sets whether the demo item is featured.
        /// </summary>
        /// <param name="isFeatured">Whether the demo item is featured.</param>
        public void SetFeatured(bool isFeatured)
        {
            if (IsFeatured != isFeatured)
            {
                IsFeatured = isFeatured;
                AddDomainEvent(new DemoItemFeaturedChangedEvent(Id, isFeatured));
            }
        }
    }

    /// <summary>
    /// Event raised when a demo item is created.
    /// </summary>
    public class DemoItemCreatedEvent : DomainEvent
    {
        /// <summary>
        /// Gets the ID of the demo item.
        /// </summary>
        public Guid DemoItemId { get; }

        /// <summary>
        /// Gets the name of the demo item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoItemCreatedEvent"/> class.
        /// </summary>
        /// <param name="demoItemId">The ID of the demo item.</param>
        /// <param name="name">The name of the demo item.</param>
        public DemoItemCreatedEvent(Guid demoItemId, string name)
        {
            DemoItemId = demoItemId;
            Name = name;
        }
    }

    /// <summary>
    /// Event raised when a demo item is updated.
    /// </summary>
    public class DemoItemUpdatedEvent : DomainEvent
    {
        /// <summary>
        /// Gets the ID of the demo item.
        /// </summary>
        public Guid DemoItemId { get; }

        /// <summary>
        /// Gets the name of the demo item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoItemUpdatedEvent"/> class.
        /// </summary>
        /// <param name="demoItemId">The ID of the demo item.</param>
        /// <param name="name">The name of the demo item.</param>
        public DemoItemUpdatedEvent(Guid demoItemId, string name)
        {
            DemoItemId = demoItemId;
            Name = name;
        }
    }

    /// <summary>
    /// Event raised when a demo item's featured status is changed.
    /// </summary>
    public class DemoItemFeaturedChangedEvent : DomainEvent
    {
        /// <summary>
        /// Gets the ID of the demo item.
        /// </summary>
        public Guid DemoItemId { get; }

        /// <summary>
        /// Gets a value indicating whether the demo item is featured.
        /// </summary>
        public bool IsFeatured { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoItemFeaturedChangedEvent"/> class.
        /// </summary>
        /// <param name="demoItemId">The ID of the demo item.</param>
        /// <param name="isFeatured">Whether the demo item is featured.</param>
        public DemoItemFeaturedChangedEvent(Guid demoItemId, bool isFeatured)
        {
            DemoItemId = demoItemId;
            IsFeatured = isFeatured;
        }
    }
}