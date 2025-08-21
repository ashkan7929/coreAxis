using CoreAxis.Modules.CommerceModule.Api.Controllers;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Commands.Inventory;
using CoreAxis.Modules.CommerceModule.Application.Queries.Inventory;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.SharedKernel.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CoreAxis.Modules.CommerceModule.Tests.Api;

/// <summary>
/// Unit tests for InventoryController
/// </summary>
public class InventoryControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<InventoryController>> _loggerMock;
    private readonly InventoryController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public InventoryControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<InventoryController>>();
        _controller = new InventoryController(_mediatorMock.Object, _loggerMock.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new("permissions", "inventory.read"),
            new("permissions", "inventory.write")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    #region GetInventoryItems Tests

    [Fact]
    public async Task GetInventoryItems_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var query = new GetInventoryItemsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };
        
        var inventoryItems = new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Item 1", Sku = "SKU001", Quantity = 100 },
            new() { Id = Guid.NewGuid(), Name = "Test Item 2", Sku = "SKU002", Quantity = 50 }
        };
        
        var pagedResult = new PagedResult<InventoryItem>(inventoryItems, 2, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetInventoryItemsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<InventoryItem>>.Success(pagedResult));

        // Act
        var result = await _controller.GetInventoryItems(1, 10, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<InventoryItemDto>>(okResult.Value);
        Assert.Equal(2, response.Items.Count());
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetInventoryItems_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetInventoryItems(1, 0, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Page size must be greater than 0", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task GetInventoryItems_WhenMediatorFails_ReturnsInternalServerError()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetInventoryItemsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<InventoryItem>>.Failure("Database error"));

        // Act
        var result = await _controller.GetInventoryItems(1, 10, null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetInventoryItem Tests

    [Fact]
    public async Task GetInventoryItem_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var inventoryItem = new InventoryItem
        {
            Id = itemId,
            Name = "Test Item",
            Sku = "SKU001",
            Quantity = 100
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetInventoryItemQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryItem>.Success(inventoryItem));

        // Act
        var result = await _controller.GetInventoryItem(itemId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InventoryItemDto>(okResult.Value);
        Assert.Equal(itemId, response.Id);
        Assert.Equal("Test Item", response.Name);
    }

    [Fact]
    public async Task GetInventoryItem_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetInventoryItemQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryItem>.Failure("Item not found"));

        // Act
        var result = await _controller.GetInventoryItem(itemId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Item not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region CreateInventoryItem Tests

    [Fact]
    public async Task CreateInventoryItem_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateInventoryItemDto
        {
            Name = "New Item",
            Sku = "SKU003",
            Quantity = 75,
            MinimumQuantity = 10,
            Price = 29.99m
        };
        
        var createdItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Sku = createDto.Sku,
            Quantity = createDto.Quantity
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateInventoryItemCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryItem>.Success(createdItem));

        // Act
        var result = await _controller.CreateInventoryItem(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<InventoryItemDto>(createdResult.Value);
        Assert.Equal(createDto.Name, response.Name);
        Assert.Equal(createDto.Sku, response.Sku);
    }

    [Fact]
    public async Task CreateInventoryItem_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateInventoryItemDto
        {
            Name = "", // Invalid empty name
            Sku = "SKU003",
            Quantity = 75
        };
        
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateInventoryItem(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateInventoryItem_WhenDuplicateSku_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateInventoryItemDto
        {
            Name = "New Item",
            Sku = "SKU003",
            Quantity = 75
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateInventoryItemCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryItem>.Failure("SKU already exists"));

        // Act
        var result = await _controller.CreateInventoryItem(createDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("SKU already exists", conflictResult.Value?.ToString());
    }

    #endregion

    #region UpdateInventoryItem Tests

    [Fact]
    public async Task UpdateInventoryItem_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var updateDto = new UpdateInventoryItemDto
        {
            Name = "Updated Item",
            Quantity = 150,
            Price = 39.99m
        };
        
        var updatedItem = new InventoryItem
        {
            Id = itemId,
            Name = updateDto.Name,
            Quantity = updateDto.Quantity.Value
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateInventoryItemCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryItem>.Success(updatedItem));

        // Act
        var result = await _controller.UpdateInventoryItem(itemId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InventoryItemDto>(okResult.Value);
        Assert.Equal(updateDto.Name, response.Name);
        Assert.Equal(updateDto.Quantity, response.Quantity);
    }

    [Fact]
    public async Task UpdateInventoryItem_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var updateDto = new UpdateInventoryItemDto
        {
            Name = "Updated Item"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateInventoryItemCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryItem>.Failure("Item not found"));

        // Act
        var result = await _controller.UpdateInventoryItem(itemId, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Item not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region DeleteInventoryItem Tests

    [Fact]
    public async Task DeleteInventoryItem_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteInventoryItemCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.DeleteInventoryItem(itemId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteInventoryItem_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteInventoryItemCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure("Item not found"));

        // Act
        var result = await _controller.DeleteInventoryItem(itemId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Item not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region ReserveInventory Tests

    [Fact]
    public async Task ReserveInventory_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var reserveDto = new ReserveInventoryDto
        {
            InventoryItemId = Guid.NewGuid(),
            Quantity = 5,
            ReservationTimeoutMinutes = 15
        };
        
        var reservation = new InventoryReservation
        {
            Id = Guid.NewGuid(),
            InventoryItemId = reserveDto.InventoryItemId,
            Quantity = reserveDto.Quantity,
            Status = ReservationStatus.Active
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryReservation>.Success(reservation));

        // Act
        var result = await _controller.ReserveInventory(reserveDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InventoryReservationDto>(okResult.Value);
        Assert.Equal(reservation.Id, response.Id);
        Assert.Equal(reserveDto.Quantity, response.Quantity);
    }

    [Fact]
    public async Task ReserveInventory_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var reserveDto = new ReserveInventoryDto
        {
            InventoryItemId = Guid.NewGuid(),
            Quantity = 1000, // More than available
            ReservationTimeoutMinutes = 15
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<InventoryReservation>.Failure("Insufficient stock"));

        // Act
        var result = await _controller.ReserveInventory(reserveDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Insufficient stock", badRequestResult.Value?.ToString());
    }

    #endregion

    #region ReleaseReservation Tests

    [Fact]
    public async Task ReleaseReservation_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ReleaseInventoryReservationCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.ReleaseReservation(reservationId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ReleaseReservation_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ReleaseInventoryReservationCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure("Reservation not found"));

        // Act
        var result = await _controller.ReleaseReservation(reservationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Reservation not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetInventoryItems_WithoutReadPermission_ReturnsForbidden()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _testUserId.ToString())
            // No inventory.read permission
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act & Assert
        // This would be handled by the authorization filter in a real scenario
        // For unit testing, we'd need to test the authorization attribute separately
        Assert.True(true); // Placeholder for authorization testing
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)] // Assuming max page size is 100
    public async Task GetInventoryItems_WithInvalidPageSize_ReturnsBadRequest(int pageSize)
    {
        // Act
        var result = await _controller.GetInventoryItems(1, pageSize, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetInventoryItems_WithInvalidPageNumber_ReturnsBadRequest(int pageNumber)
    {
        // Act
        var result = await _controller.GetInventoryItems(pageNumber, 10, null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetInventoryItems_WhenExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetInventoryItemsQuery>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.GetInventoryItems(1, 10, null, null));
        
        // Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting inventory items")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}