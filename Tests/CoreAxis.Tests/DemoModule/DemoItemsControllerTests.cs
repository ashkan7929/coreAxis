using CoreAxis.Modules.DemoModule.API;
using CoreAxis.Modules.DemoModule.Application;
using CoreAxis.Modules.DemoModule.Domain;
using CoreAxis.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.DemoModule
{
    /// <summary>
    /// Unit tests for the DemoItemsController.
    /// </summary>
    public class DemoItemsControllerTests
    {
        private readonly Mock<IDemoItemService> _mockService;
        private readonly DemoItemsController _controller;

        public DemoItemsControllerTests()
        {
            _mockService = new Mock<IDemoItemService>();
            _controller = new DemoItemsController(_mockService.Object);
        }

        /// <summary>
        /// Tests that GetAll returns a successful response with paginated DemoItems.
        /// </summary>
        [Fact]
        public async Task GetAll_ShouldReturnOkResultWithPaginatedItems()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;
            var demoItems = new List<DemoItem>
            {
                DemoItem.Create("Item 1", "Description 1", 10.99m, "Category 1"),
                DemoItem.Create("Item 2", "Description 2", 20.99m, "Category 2")
            };
            var paginatedList = PaginatedList<DemoItem>.Create(demoItems, pageNumber, pageSize, demoItems.Count);

            _mockService.Setup(service => service.GetAllAsync(pageNumber, pageSize))
                .ReturnsAsync(paginatedList);

            // Act
            var result = await _controller.GetAll(pageNumber, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<PaginatedList<DemoItem>>(okResult.Value);
            Assert.Equal(demoItems.Count, returnValue.Items.Count);
            Assert.Equal(pageNumber, returnValue.PageNumber);
            Assert.Equal(pageSize, returnValue.PageSize);
        }

        /// <summary>
        /// Tests that GetById returns a successful response with the DemoItem when it exists.
        /// </summary>
        [Fact]
        public async Task GetById_WithExistingId_ShouldReturnOkResultWithDemoItem()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var demoItem = DemoItem.Create("Test Item", "Description", 10.99m, "Category");
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(demoItem, demoItemId);

            _mockService.Setup(service => service.GetByIdAsync(demoItemId))
                .ReturnsAsync(Result<DemoItem>.Success(demoItem));

            // Act
            var result = await _controller.GetById(demoItemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<DemoItem>(okResult.Value);
            Assert.Equal(demoItemId, returnValue.Id);
        }

        /// <summary>
        /// Tests that GetById returns a NotFound response when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task GetById_WithNonExistingId_ShouldReturnNotFoundResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            string errorMessage = "DemoItem not found";

            _mockService.Setup(service => service.GetByIdAsync(demoItemId))
                .ReturnsAsync(Result<DemoItem>.Failure(errorMessage));

            // Act
            var result = await _controller.GetById(demoItemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(errorMessage, notFoundResult.Value);
        }

        /// <summary>
        /// Tests that GetByCategory returns a successful response with paginated DemoItems filtered by category.
        /// </summary>
        [Fact]
        public async Task GetByCategory_ShouldReturnOkResultWithFilteredItems()
        {
            // Arrange
            string category = "Test Category";
            int pageNumber = 1;
            int pageSize = 10;
            var demoItems = new List<DemoItem>
            {
                DemoItem.Create("Item 1", "Description 1", 10.99m, category),
                DemoItem.Create("Item 2", "Description 2", 20.99m, category)
            };
            var paginatedList = PaginatedList<DemoItem>.Create(demoItems, pageNumber, pageSize, demoItems.Count);

            _mockService.Setup(service => service.GetByCategoryAsync(category, pageNumber, pageSize))
                .ReturnsAsync(paginatedList);

            // Act
            var result = await _controller.GetByCategory(category, pageNumber, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<PaginatedList<DemoItem>>(okResult.Value);
            Assert.Equal(demoItems.Count, returnValue.Items.Count);
            Assert.All(returnValue.Items, item => Assert.Equal(category, item.Category));
        }

        /// <summary>
        /// Tests that GetFeatured returns a successful response with paginated featured DemoItems.
        /// </summary>
        [Fact]
        public async Task GetFeatured_ShouldReturnOkResultWithFeaturedItems()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;
            var demoItems = new List<DemoItem>
            {
                DemoItem.Create("Item 1", "Description 1", 10.99m, "Category 1"),
                DemoItem.Create("Item 2", "Description 2", 20.99m, "Category 2")
            };
            
            // Set items as featured
            foreach (var item in demoItems)
            {
                item.SetFeatured(true);
            }
            
            var paginatedList = PaginatedList<DemoItem>.Create(demoItems, pageNumber, pageSize, demoItems.Count);

            _mockService.Setup(service => service.GetFeaturedAsync(pageNumber, pageSize))
                .ReturnsAsync(paginatedList);

            // Act
            var result = await _controller.GetFeatured(pageNumber, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<PaginatedList<DemoItem>>(okResult.Value);
            Assert.Equal(demoItems.Count, returnValue.Items.Count);
            Assert.All(returnValue.Items, item => Assert.True(item.IsFeatured));
        }

        /// <summary>
        /// Tests that Create returns a successful response with the created DemoItem.
        /// </summary>
        [Fact]
        public async Task Create_WithValidRequest_ShouldReturnCreatedResultWithDemoItem()
        {
            // Arrange
            var request = new CreateDemoItemRequest
            {
                Name = "Test Item",
                Description = "This is a test item",
                Price = 10.99m,
                Category = "Test Category"
            };

            var createdDemoItem = DemoItem.Create(request.Name, request.Description, request.Price, request.Category);

            _mockService.Setup(service => service.CreateAsync(request.Name, request.Description, request.Price, request.Category))
                .ReturnsAsync(Result<DemoItem>.Success(createdDemoItem));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(DemoItemsController.GetById), createdResult.ActionName);
            var returnValue = Assert.IsType<DemoItem>(createdResult.Value);
            Assert.Equal(request.Name, returnValue.Name);
            Assert.Equal(request.Description, returnValue.Description);
            Assert.Equal(request.Price, returnValue.Price);
            Assert.Equal(request.Category, returnValue.Category);
        }

        /// <summary>
        /// Tests that Create returns a BadRequest response when the service returns a failure result.
        /// </summary>
        [Fact]
        public async Task Create_WithInvalidRequest_ShouldReturnBadRequestResult()
        {
            // Arrange
            var request = new CreateDemoItemRequest
            {
                Name = "", // Invalid name
                Description = "This is a test item",
                Price = 10.99m,
                Category = "Test Category"
            };

            string errorMessage = "Name cannot be empty";

            _mockService.Setup(service => service.CreateAsync(request.Name, request.Description, request.Price, request.Category))
                .ReturnsAsync(Result<DemoItem>.Failure(errorMessage));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, badRequestResult.Value);
        }

        /// <summary>
        /// Tests that Update returns a successful response with the updated DemoItem when it exists.
        /// </summary>
        [Fact]
        public async Task Update_WithExistingId_ShouldReturnOkResultWithDemoItem()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var request = new UpdateDemoItemRequest
            {
                Name = "Updated Item",
                Description = "This is an updated item",
                Price = 20.99m,
                Category = "Updated Category"
            };

            var updatedDemoItem = DemoItem.Create(request.Name, request.Description, request.Price, request.Category);
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(updatedDemoItem, demoItemId);

            _mockService.Setup(service => service.UpdateAsync(demoItemId, request.Name, request.Description, request.Price, request.Category))
                .ReturnsAsync(Result<DemoItem>.Success(updatedDemoItem));

            // Act
            var result = await _controller.Update(demoItemId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<DemoItem>(okResult.Value);
            Assert.Equal(demoItemId, returnValue.Id);
            Assert.Equal(request.Name, returnValue.Name);
            Assert.Equal(request.Description, returnValue.Description);
            Assert.Equal(request.Price, returnValue.Price);
            Assert.Equal(request.Category, returnValue.Category);
        }

        /// <summary>
        /// Tests that Update returns a NotFound response when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task Update_WithNonExistingId_ShouldReturnNotFoundResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var request = new UpdateDemoItemRequest
            {
                Name = "Updated Item",
                Description = "This is an updated item",
                Price = 20.99m,
                Category = "Updated Category"
            };

            string errorMessage = "DemoItem not found";

            _mockService.Setup(service => service.UpdateAsync(demoItemId, request.Name, request.Description, request.Price, request.Category))
                .ReturnsAsync(Result<DemoItem>.Failure(errorMessage));

            // Act
            var result = await _controller.Update(demoItemId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(errorMessage, notFoundResult.Value);
        }

        /// <summary>
        /// Tests that SetFeatured returns a successful response with the updated DemoItem when it exists.
        /// </summary>
        [Fact]
        public async Task SetFeatured_WithExistingId_ShouldReturnOkResultWithDemoItem()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var request = new SetFeaturedRequest { IsFeatured = true };

            var demoItem = DemoItem.Create("Test Item", "Description", 10.99m, "Category");
            demoItem.SetFeatured(request.IsFeatured);
            
            // Use reflection to set the Id property
            typeof(DemoItem).GetProperty("Id").SetValue(demoItem, demoItemId);

            _mockService.Setup(service => service.SetFeaturedAsync(demoItemId, request.IsFeatured))
                .ReturnsAsync(Result<DemoItem>.Success(demoItem));

            // Act
            var result = await _controller.SetFeatured(demoItemId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<DemoItem>(okResult.Value);
            Assert.Equal(demoItemId, returnValue.Id);
            Assert.Equal(request.IsFeatured, returnValue.IsFeatured);
        }

        /// <summary>
        /// Tests that SetFeatured returns a NotFound response when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task SetFeatured_WithNonExistingId_ShouldReturnNotFoundResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            var request = new SetFeaturedRequest { IsFeatured = true };

            string errorMessage = "DemoItem not found";

            _mockService.Setup(service => service.SetFeaturedAsync(demoItemId, request.IsFeatured))
                .ReturnsAsync(Result<DemoItem>.Failure(errorMessage));

            // Act
            var result = await _controller.SetFeatured(demoItemId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(errorMessage, notFoundResult.Value);
        }

        /// <summary>
        /// Tests that Delete returns a successful response when the DemoItem exists.
        /// </summary>
        [Fact]
        public async Task Delete_WithExistingId_ShouldReturnNoContentResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();

            _mockService.Setup(service => service.DeleteAsync(demoItemId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Delete(demoItemId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        /// <summary>
        /// Tests that Delete returns a NotFound response when the DemoItem doesn't exist.
        /// </summary>
        [Fact]
        public async Task Delete_WithNonExistingId_ShouldReturnNotFoundResult()
        {
            // Arrange
            var demoItemId = Guid.NewGuid();
            string errorMessage = "DemoItem not found";

            _mockService.Setup(service => service.DeleteAsync(demoItemId))
                .ReturnsAsync(Result.Failure(errorMessage));

            // Act
            var result = await _controller.Delete(demoItemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(errorMessage, notFoundResult.Value);
        }
    }
}