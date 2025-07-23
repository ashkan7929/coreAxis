using CoreAxis.Modules.DemoModule.Domain;
using System;
using Xunit;

namespace CoreAxis.Tests.DemoModule
{
    /// <summary>
    /// Unit tests for the DemoItem entity.
    /// </summary>
    public class DemoItemTests
    {
        /// <summary>
        /// Tests that a DemoItem can be created with valid parameters.
        /// </summary>
        [Fact]
        public void Create_WithValidParameters_ShouldCreateDemoItem()
        {
            // Arrange
            string name = "Test Item";
            string description = "This is a test item";
            decimal price = 10.99m;
            string category = "Test Category";

            // Act
            var demoItem = DemoItem.Create(name, description, price, category);

            // Assert
            Assert.NotNull(demoItem);
            Assert.Equal(name, demoItem.Name);
            Assert.Equal(description, demoItem.Description);
            Assert.Equal(price, demoItem.Price);
            Assert.Equal(category, demoItem.Category);
            Assert.False(demoItem.IsFeatured);
            Assert.NotEqual(Guid.Empty, demoItem.Id);
            Assert.Single(demoItem.DomainEvents);
            Assert.IsType<DemoItemCreatedEvent>(demoItem.DomainEvents[0]);
        }

        /// <summary>
        /// Tests that creating a DemoItem with an empty name throws an ArgumentException.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void Create_WithEmptyName_ShouldThrowArgumentException(string name)
        {
            // Arrange
            string description = "This is a test item";
            decimal price = 10.99m;
            string category = "Test Category";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => DemoItem.Create(name, description, price, category));
            Assert.Equal("name", exception.ParamName);
        }

        /// <summary>
        /// Tests that creating a DemoItem with a negative price throws an ArgumentException.
        /// </summary>
        [Fact]
        public void Create_WithNegativePrice_ShouldThrowArgumentException()
        {
            // Arrange
            string name = "Test Item";
            string description = "This is a test item";
            decimal price = -10.99m;
            string category = "Test Category";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => DemoItem.Create(name, description, price, category));
            Assert.Equal("price", exception.ParamName);
        }

        /// <summary>
        /// Tests that a DemoItem can be updated with valid parameters.
        /// </summary>
        [Fact]
        public void Update_WithValidParameters_ShouldUpdateDemoItem()
        {
            // Arrange
            var demoItem = DemoItem.Create("Test Item", "This is a test item", 10.99m, "Test Category");
            string newName = "Updated Item";
            string newDescription = "This is an updated item";
            decimal newPrice = 20.99m;
            string newCategory = "Updated Category";

            // Clear domain events from creation
            demoItem.ClearDomainEvents();

            // Act
            demoItem.Update(newName, newDescription, newPrice, newCategory);

            // Assert
            Assert.Equal(newName, demoItem.Name);
            Assert.Equal(newDescription, demoItem.Description);
            Assert.Equal(newPrice, demoItem.Price);
            Assert.Equal(newCategory, demoItem.Category);
            Assert.Single(demoItem.DomainEvents);
            Assert.IsType<DemoItemUpdatedEvent>(demoItem.DomainEvents[0]);
        }

        /// <summary>
        /// Tests that updating a DemoItem with an empty name throws an ArgumentException.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void Update_WithEmptyName_ShouldThrowArgumentException(string name)
        {
            // Arrange
            var demoItem = DemoItem.Create("Test Item", "This is a test item", 10.99m, "Test Category");
            string newDescription = "This is an updated item";
            decimal newPrice = 20.99m;
            string newCategory = "Updated Category";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => demoItem.Update(name, newDescription, newPrice, newCategory));
            Assert.Equal("name", exception.ParamName);
        }

        /// <summary>
        /// Tests that updating a DemoItem with a negative price throws an ArgumentException.
        /// </summary>
        [Fact]
        public void Update_WithNegativePrice_ShouldThrowArgumentException()
        {
            // Arrange
            var demoItem = DemoItem.Create("Test Item", "This is a test item", 10.99m, "Test Category");
            string newName = "Updated Item";
            string newDescription = "This is an updated item";
            decimal newPrice = -20.99m;
            string newCategory = "Updated Category";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => demoItem.Update(newName, newDescription, newPrice, newCategory));
            Assert.Equal("price", exception.ParamName);
        }

        /// <summary>
        /// Tests that a DemoItem's featured status can be set.
        /// </summary>
        [Fact]
        public void SetFeatured_ShouldSetFeaturedStatus()
        {
            // Arrange
            var demoItem = DemoItem.Create("Test Item", "This is a test item", 10.99m, "Test Category");
            
            // Clear domain events from creation
            demoItem.ClearDomainEvents();

            // Act
            demoItem.SetFeatured(true);

            // Assert
            Assert.True(demoItem.IsFeatured);
            Assert.Single(demoItem.DomainEvents);
            Assert.IsType<DemoItemFeaturedChangedEvent>(demoItem.DomainEvents[0]);
            var @event = (DemoItemFeaturedChangedEvent)demoItem.DomainEvents[0];
            Assert.Equal(demoItem.Id, @event.DemoItemId);
            Assert.True(@event.IsFeatured);

            // Clear domain events
            demoItem.ClearDomainEvents();

            // Act again - setting to the same value should not raise an event
            demoItem.SetFeatured(true);

            // Assert
            Assert.True(demoItem.IsFeatured);
            Assert.Empty(demoItem.DomainEvents);

            // Act again - setting to a different value should raise an event
            demoItem.SetFeatured(false);

            // Assert
            Assert.False(demoItem.IsFeatured);
            Assert.Single(demoItem.DomainEvents);
            Assert.IsType<DemoItemFeaturedChangedEvent>(demoItem.DomainEvents[0]);
            @event = (DemoItemFeaturedChangedEvent)demoItem.DomainEvents[0];
            Assert.Equal(demoItem.Id, @event.DemoItemId);
            Assert.False(@event.IsFeatured);
        }
    }
}