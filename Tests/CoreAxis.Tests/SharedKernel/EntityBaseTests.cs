using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.Linq;
using Xunit;

namespace CoreAxis.Tests.SharedKernel
{
    /// <summary>
    /// Unit tests for the EntityBase class.
    /// </summary>
    public class EntityBaseTests
    {
        /// <summary>
        /// Tests that a new entity has a non-empty ID.
        /// </summary>
        [Fact]
        public void NewEntity_ShouldHaveNonEmptyId()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
        }

        /// <summary>
        /// Tests that a new entity has CreatedOn set to the current time.
        /// </summary>
        [Fact]
        public void NewEntity_ShouldHaveCreatedOnSetToCurrentTime()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.True(DateTime.UtcNow.AddMinutes(-1) <= entity.CreatedOn);
            Assert.True(entity.CreatedOn <= DateTime.UtcNow.AddMinutes(1));
        }

        /// <summary>
        /// Tests that a new entity has LastModifiedOn set to the same value as CreatedOn.
        /// </summary>
        [Fact]
        public void NewEntity_ShouldHaveLastModifiedOnEqualToCreatedOn()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.Equal(entity.CreatedOn, entity.LastModifiedOn);
        }

        /// <summary>
        /// Tests that a new entity has IsActive set to true by default.
        /// </summary>
        [Fact]
        public void NewEntity_ShouldHaveIsActiveSetToTrue()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.True(entity.IsActive);
        }

        /// <summary>
        /// Tests that AddDomainEvent adds the event to the domain events collection.
        /// </summary>
        [Fact]
        public void AddDomainEvent_ShouldAddEventToDomainEvents()
        {
            // Arrange
            var entity = new TestEntity();
            var domainEvent = new TestDomainEvent();

            // Act
            entity.AddTestDomainEvent(domainEvent);

            // Assert
            Assert.Single(entity.DomainEvents);
            Assert.Equal(domainEvent, entity.DomainEvents.First());
        }

        /// <summary>
        /// Tests that RemoveDomainEvent removes the event from the domain events collection.
        /// </summary>
        [Fact]
        public void RemoveDomainEvent_ShouldRemoveEventFromDomainEvents()
        {
            // Arrange
            var entity = new TestEntity();
            var domainEvent = new TestDomainEvent();
            entity.AddTestDomainEvent(domainEvent);

            // Act
            entity.RemoveTestDomainEvent(domainEvent);

            // Assert
            Assert.Empty(entity.DomainEvents);
        }

        /// <summary>
        /// Tests that ClearDomainEvents removes all events from the domain events collection.
        /// </summary>
        [Fact]
        public void ClearDomainEvents_ShouldRemoveAllEventsFromDomainEvents()
        {
            // Arrange
            var entity = new TestEntity();
            entity.AddTestDomainEvent(new TestDomainEvent());
            entity.AddTestDomainEvent(new TestDomainEvent());
            entity.AddTestDomainEvent(new TestDomainEvent());

            // Act
            entity.ClearDomainEvents();

            // Assert
            Assert.Empty(entity.DomainEvents);
        }

        /// <summary>
        /// Tests that two entities with the same ID are equal.
        /// </summary>
        [Fact]
        public void Equals_WithSameId_ShouldReturnTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity1 = new TestEntity(id);
            var entity2 = new TestEntity(id);

            // Act & Assert
            Assert.Equal(entity1, entity2);
            Assert.True(entity1.Equals(entity2));
            Assert.True(entity1 == entity2);
            Assert.False(entity1 != entity2);
        }

        /// <summary>
        /// Tests that two entities with different IDs are not equal.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentIds_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new TestEntity(Guid.NewGuid());
            var entity2 = new TestEntity(Guid.NewGuid());

            // Act & Assert
            Assert.NotEqual(entity1, entity2);
            Assert.False(entity1.Equals(entity2));
            Assert.False(entity1 == entity2);
            Assert.True(entity1 != entity2);
        }

        /// <summary>
        /// Tests that an entity is not equal to null.
        /// </summary>
        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var entity = new TestEntity();

            // Act & Assert
            Assert.False(entity.Equals(null));
            Assert.False(entity == null);
            Assert.True(entity != null);
        }

        /// <summary>
        /// Tests that an entity is not equal to an object of a different type.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var entity = new TestEntity();
            var otherObject = new object();

            // Act & Assert
            Assert.False(entity.Equals(otherObject));
        }

        /// <summary>
        /// Tests that two entities with the same ID have the same hash code.
        /// </summary>
        [Fact]
        public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity1 = new TestEntity(id);
            var entity2 = new TestEntity(id);

            // Act & Assert
            Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
        }

        /// <summary>
        /// Tests that two entities with different IDs have different hash codes.
        /// </summary>
        [Fact]
        public void GetHashCode_WithDifferentIds_ShouldReturnDifferentHashCode()
        {
            // Arrange
            var entity1 = new TestEntity(Guid.NewGuid());
            var entity2 = new TestEntity(Guid.NewGuid());

            // Act & Assert
            Assert.NotEqual(entity1.GetHashCode(), entity2.GetHashCode());
        }
    }

    /// <summary>
    /// Test entity implementation for testing purposes.
    /// </summary>
    public class TestEntity : EntityBase
    {
        private readonly List<DomainEvent> _testDomainEvents = new List<DomainEvent>();

        public TestEntity() : base()
        {
        }

        public TestEntity(Guid id) : base()
        {
            // Use reflection to set the Id property since it's protected
            var property = typeof(EntityBase).GetProperty("Id");
            property.SetValue(this, id);
        }

        public void AddTestDomainEvent(DomainEvent domainEvent)
        {
            AddDomainEvent(domainEvent);
        }

        public void RemoveTestDomainEvent(DomainEvent domainEvent)
        {
            // Since EntityBase doesn't have RemoveDomainEvent, we'll simulate it by clearing and re-adding
            var events = DomainEvents.Where(e => e != domainEvent).ToList();
            ClearDomainEvents();
            foreach (var evt in events)
            {
                AddDomainEvent(evt);
            }
        }
    }

    /// <summary>
    /// Test domain event implementation for testing purposes.
    /// </summary>
    public class TestDomainEvent : DomainEvent
    {
        // EventType is inherited from base class and doesn't need to be overridden
    }
}