using CoreAxis.Modules.DemoModule.Application;
using CoreAxis.Modules.DemoModule.Domain;
using CoreAxis.SharedKernel;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.DemoModule
{
    /// <summary>
    /// Unit tests for the DemoItemService.
    /// </summary>
    public class DemoItemServiceTests
    {
        private readonly Mock<IDemoItemRepository> _mockRepository;
        private readonly DemoItemService _service;

        public DemoItemServiceTests()
        {
            _mockRepository = new Mock<IDemoItemRepository>();
            _service = new DemoItemService(_mockRepository.Object);
        }

        /// <summary>
        /// Tests that GetByIdAsync returns a successful result with the correct DemoItem when it exists.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnSuccessResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var demoItem = DemoItem.Create("Test Item", "Description", 10.99m, "Category");
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(demoItem, demoItemId);
            
            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync(demoItem);

            // Act
            var result = await _service.GetByIdAsync(demoItemId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(demoItem, result.Value);
        }

        /// <summary>
        /// Tests that GetByIdAsync returns a failure result when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnFailureResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync((DemoItem)null);

            // Act
            var result = await _service.GetByIdAsync(demoItemId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that GetAllAsync returns a paginated list of DemoItems.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ShouldReturnPaginatedList()
        {
            // Arrange
            var demoItems = new List<DemoItem>
            {
                DemoItem.Create("Item 1", "Description 1", 10.99m, "Category 1"),
                DemoItem.Create("Item 2", "Description 2", 20.99m, "Category 2"),
                DemoItem.Create("Item 3", "Description 3", 30.99m, "Category 3"),
                DemoItem.Create("Item 4", "Description 4", 40.99m, "Category 4"),
                DemoItem.Create("Item 5", "Description 5", 50.99m, "Category 5")
            };

            _mockRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(demoItems);

            // Act
            var result = await _service.GetAllAsync(2, 2); // Page 2, Page Size 2

            // Assert
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(demoItems[2].Name, result.Items[0].Name); // First item on page 2 should be Item 3
            Assert.Equal(demoItems[3].Name, result.Items[1].Name); // Second item on page 2 should be Item 4
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
        }

        /// <summary>
        /// Tests that GetByCategoryAsync returns DemoItems filtered by category.
        /// </summary>
        [Fact]
        public async Task GetByCategoryAsync_ShouldReturnFilteredItems()
        {
            // Arrange
            string category = "Category 1";
            var demoItems = new List<DemoItem>
            {
                DemoItem.Create("Item 1", "Description 1", 10.99m, category),
                DemoItem.Create("Item 2", "Description 2", 20.99m, category),
                DemoItem.Create("Item 3", "Description 3", 30.99m, "Category 2")
            };

            var filteredItems = demoItems.Where(i => i.Category == category).ToList();

            _mockRepository.Setup(repo => repo.GetByCategoryAsync(category))
                .ReturnsAsync(filteredItems);

            // Act
            var result = await _service.GetByCategoryAsync(category, 1, 10);

            // Assert
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.TotalPages);
            Assert.All(result.Items, item => Assert.Equal(category, item.Category));
        }

        /// <summary>
        /// Tests that GetFeaturedAsync returns only featured DemoItems.
        /// </summary>
        [Fact]
        public async Task GetFeaturedAsync_ShouldReturnFeaturedItems()
        {
            // Arrange
            var demoItems = new List<DemoItem>
            {
                DemoItem.Create("Item 1", "Description 1", 10.99m, "Category 1"),
                DemoItem.Create("Item 2", "Description 2", 20.99m, "Category 2"),
                DemoItem.Create("Item 3", "Description 3", 30.99m, "Category 3")
            };

            // Set some items as featured
            demoItems[0].SetFeatured(true);
            demoItems[2].SetFeatured(true);

            var featuredItems = demoItems.Where(i => i.IsFeatured).ToList();

            _mockRepository.Setup(repo => repo.GetFeaturedAsync())
                .ReturnsAsync(featuredItems);

            // Act
            var result = await _service.GetFeaturedAsync(1, 10);

            // Assert
            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.True(item.IsFeatured));
        }

        /// <summary>
        /// Tests that CreateAsync returns a successful result with the created DemoItem.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithValidParameters_ShouldReturnSuccessResult()
        {
            // Arrange
            string name = "Test Item";
            string description = "This is a test item";
            decimal price = 10.99m;
            string category = "Test Category";

            DemoItem savedDemoItem = null;

            _mockRepository.Setup(repo => repo.AddAsync(It.IsAny<DemoItem>()))
                .Callback<DemoItem>(item => savedDemoItem = item)
                .ReturnsAsync((DemoItem item) => item);

            // Act
            var result = await _service.CreateAsync(name, description, price, category);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(name, result.Value.Name);
            Assert.Equal(description, result.Value.Description);
            Assert.Equal(price, result.Value.Price);
            Assert.Equal(category, result.Value.Category);
            Assert.False(result.Value.IsFeatured);

            // Verify repository was called
            _mockRepository.Verify(repo => repo.AddAsync(It.IsAny<DemoItem>()), Times.Once);
        }

        /// <summary>
        /// Tests that UpdateAsync returns a successful result with the updated DemoItem when it exists.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithExistingId_ShouldReturnSuccessResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var demoItem = DemoItem.Create("Test Item", "Description", 10.99m, "Category");
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(demoItem, demoItemId);
            
            string newName = "Updated Item";
            string newDescription = "Updated description";
            decimal newPrice = 20.99m;
            string newCategory = "Updated Category";

            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync(demoItem);

            _mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<DemoItem>()))
                .ReturnsAsync((DemoItem item) => item);

            // Act
            var result = await _service.UpdateAsync(demoItemId, newName, newDescription, newPrice, newCategory);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newName, result.Value.Name);
            Assert.Equal(newDescription, result.Value.Description);
            Assert.Equal(newPrice, result.Value.Price);
            Assert.Equal(newCategory, result.Value.Category);

            // Verify repository was called
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<DemoItem>()), Times.Once);
        }

        /// <summary>
        /// Tests that UpdateAsync returns a failure result when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithNonExistingId_ShouldReturnFailureResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            
            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync((DemoItem)null);

            // Act
            var result = await _service.UpdateAsync(demoItemId, "Updated Item", "Updated description", 20.99m, "Updated Category");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);

            // Verify repository was not called for update
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<DemoItem>()), Times.Never);
        }

        /// <summary>
        /// Tests that SetFeaturedAsync returns a successful result with the updated DemoItem when it exists.
        /// </summary>
        [Fact]
        public async Task SetFeaturedAsync_WithExistingId_ShouldReturnSuccessResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var demoItem = DemoItem.Create("Test Item", "Description", 10.99m, "Category");
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(demoItem, demoItemId);
            
            bool isFeatured = true;

            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync(demoItem);

            _mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<DemoItem>()))
                .ReturnsAsync((DemoItem item) => item);

            // Act
            var result = await _service.SetFeaturedAsync(demoItemId, isFeatured);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(isFeatured, result.Value.IsFeatured);

            // Verify repository was called
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<DemoItem>()), Times.Once);
        }

        /// <summary>
        /// Tests that DeleteAsync returns a successful result when the DemoItem exists.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WithExistingId_ShouldReturnSuccessResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var demoItem = DemoItem.Create("Test Item", "Description", 10.99m, "Category");
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(demoItem, demoItemId);

            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync(demoItem);

            _mockRepository.Setup(repo => repo.DeleteAsync(It.IsAny<DemoItem>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(demoItemId);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify repository was called
            _mockRepository.Verify(repo => repo.DeleteAsync(It.IsAny<DemoItem>()), Times.Once);
        }

        /// <summary>
        /// Tests that DeleteAsync returns a failure result when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WithNonExistingId_ShouldReturnFailureResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            
            _mockRepository.Setup(repo => repo.GetByIdAsync(demoItemId))
                .ReturnsAsync((DemoItem)null);

            // Act
            var result = await _service.DeleteAsync(demoItemId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);

            // Verify repository was not called for delete
            _mockRepository.Verify(repo => repo.DeleteAsync(It.IsAny<DemoItem>()), Times.Never);
        }
    }
}