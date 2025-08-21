using CoreAxis.Modules.CommerceModule.Api.Controllers;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Commands.Orders;
using CoreAxis.Modules.CommerceModule.Application.Queries.Orders;
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
/// Unit tests for OrderController
/// </summary>
public class OrderControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<OrderController>> _loggerMock;
    private readonly OrderController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testCustomerId = Guid.NewGuid();

    public OrderControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<OrderController>>();
        _controller = new OrderController(_mediatorMock.Object, _loggerMock.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new("customer_id", _testCustomerId.ToString()),
            new("permissions", "orders.read"),
            new("permissions", "orders.write"),
            new("permissions", "orders.manage")
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

    #region GetOrders Tests

    [Fact]
    public async Task GetOrders_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                CustomerId = _testCustomerId, 
                Status = OrderStatus.Pending,
                TotalAmount = 100.00m,
                CreatedAt = DateTime.UtcNow
            },
            new() 
            { 
                Id = Guid.NewGuid(), 
                CustomerId = _testCustomerId, 
                Status = OrderStatus.Confirmed,
                TotalAmount = 250.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        
        var pagedResult = new PagedResult<Order>(orders, 2, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrdersQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Order>>.Success(pagedResult));

        // Act
        var result = await _controller.GetOrders(1, 10, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<OrderDto>>(okResult.Value);
        Assert.Equal(2, response.Items.Count());
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetOrders_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                CustomerId = _testCustomerId, 
                Status = OrderStatus.Pending,
                TotalAmount = 100.00m
            }
        };
        
        var pagedResult = new PagedResult<Order>(orders, 1, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.Is<GetOrdersQuery>(q => q.Status == OrderStatus.Pending), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Order>>.Success(pagedResult));

        // Act
        var result = await _controller.GetOrders(1, 10, OrderStatus.Pending, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<OrderDto>>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal(OrderStatus.Pending, response.Items.First().Status);
    }

    [Fact]
    public async Task GetOrders_WhenMediatorFails_ReturnsInternalServerError()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrdersQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Order>>.Failure("Database error"));

        // Act
        var result = await _controller.GetOrders(1, 10, null, null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetOrder Tests

    [Fact]
    public async Task GetOrder_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = _testCustomerId,
            Status = OrderStatus.Pending,
            TotalAmount = 150.00m,
            Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 75.00m }
            }
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrderQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(order));

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OrderDto>(okResult.Value);
        Assert.Equal(orderId, response.Id);
        Assert.Equal(_testCustomerId, response.CustomerId);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOrderQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Order not found"));

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Order not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region CreateOrder Tests

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            CustomerId = _testCustomerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 50.00m },
                new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100.00m }
            },
            ShippingAddress = "123 Test Street, Test City",
            PaymentMethod = "CreditCard"
        };
        
        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = createDto.CustomerId,
            Status = OrderStatus.Pending,
            TotalAmount = 200.00m,
            Items = createDto.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(createdOrder));

        // Act
        var result = await _controller.CreateOrder(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<OrderDto>(createdResult.Value);
        Assert.Equal(createDto.CustomerId, response.CustomerId);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(200.00m, response.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            CustomerId = Guid.Empty, // Invalid
            Items = new List<CreateOrderItemDto>() // Empty items
        };
        
        _controller.ModelState.AddModelError("CustomerId", "Customer ID is required");
        _controller.ModelState.AddModelError("Items", "At least one item is required");

        // Act
        var result = await _controller.CreateOrder(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientInventory_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            CustomerId = _testCustomerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1000, UnitPrice = 50.00m } // Too much quantity
            }
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Insufficient inventory"));

        // Act
        var result = await _controller.CreateOrder(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Insufficient inventory", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateOrder Tests

    [Fact]
    public async Task UpdateOrder_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new UpdateOrderDto
        {
            ShippingAddress = "456 Updated Street, Updated City",
            PaymentMethod = "PayPal"
        };
        
        var updatedOrder = new Order
        {
            Id = orderId,
            CustomerId = _testCustomerId,
            Status = OrderStatus.Pending,
            TotalAmount = 150.00m,
            ShippingAddress = updateDto.ShippingAddress,
            PaymentMethod = updateDto.PaymentMethod
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(updatedOrder));

        // Act
        var result = await _controller.UpdateOrder(orderId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OrderDto>(okResult.Value);
        Assert.Equal(updateDto.ShippingAddress, response.ShippingAddress);
        Assert.Equal(updateDto.PaymentMethod, response.PaymentMethod);
    }

    [Fact]
    public async Task UpdateOrder_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new UpdateOrderDto
        {
            ShippingAddress = "456 Updated Street"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Order not found"));

        // Act
        var result = await _controller.UpdateOrder(orderId, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Order not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateOrder_WhenOrderAlreadyConfirmed_ReturnsBadRequest()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateDto = new UpdateOrderDto
        {
            ShippingAddress = "456 Updated Street"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Cannot update confirmed order"));

        // Act
        var result = await _controller.UpdateOrder(orderId, updateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Cannot update confirmed order", badRequestResult.Value?.ToString());
    }

    #endregion

    #region CancelOrder Tests

    [Fact]
    public async Task CancelOrder_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cancelDto = new CancelOrderDto
        {
            Reason = "Customer requested cancellation"
        };
        
        var cancelledOrder = new Order
        {
            Id = orderId,
            CustomerId = _testCustomerId,
            Status = OrderStatus.Cancelled,
            CancellationReason = cancelDto.Reason
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(cancelledOrder));

        // Act
        var result = await _controller.CancelOrder(orderId, cancelDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OrderDto>(okResult.Value);
        Assert.Equal(OrderStatus.Cancelled, response.Status);
        Assert.Equal(cancelDto.Reason, response.CancellationReason);
    }

    [Fact]
    public async Task CancelOrder_WithInvalidReason_ReturnsBadRequest()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cancelDto = new CancelOrderDto
        {
            Reason = "" // Invalid empty reason
        };
        
        _controller.ModelState.AddModelError("Reason", "Cancellation reason is required");

        // Act
        var result = await _controller.CancelOrder(orderId, cancelDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderAlreadyFulfilled_ReturnsBadRequest()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cancelDto = new CancelOrderDto
        {
            Reason = "Customer requested cancellation"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Cannot cancel fulfilled order"));

        // Act
        var result = await _controller.CancelOrder(orderId, cancelDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Cannot cancel fulfilled order", badRequestResult.Value?.ToString());
    }

    #endregion

    #region ConfirmOrder Tests

    [Fact]
    public async Task ConfirmOrder_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        
        var confirmedOrder = new Order
        {
            Id = orderId,
            CustomerId = _testCustomerId,
            Status = OrderStatus.Confirmed,
            ConfirmedAt = DateTime.UtcNow
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConfirmOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(confirmedOrder));

        // Act
        var result = await _controller.ConfirmOrder(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OrderDto>(okResult.Value);
        Assert.Equal(OrderStatus.Confirmed, response.Status);
        Assert.NotNull(response.ConfirmedAt);
    }

    [Fact]
    public async Task ConfirmOrder_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ConfirmOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Order not found"));

        // Act
        var result = await _controller.ConfirmOrder(orderId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Order not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region FulfillOrder Tests

    [Fact]
    public async Task FulfillOrder_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var fulfillDto = new FulfillOrderDto
        {
            TrackingNumber = "TRACK123456",
            ShippingCarrier = "FedEx",
            Notes = "Package shipped successfully"
        };
        
        var fulfilledOrder = new Order
        {
            Id = orderId,
            CustomerId = _testCustomerId,
            Status = OrderStatus.Fulfilled,
            TrackingNumber = fulfillDto.TrackingNumber,
            ShippingCarrier = fulfillDto.ShippingCarrier,
            FulfilledAt = DateTime.UtcNow
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<FulfillOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(fulfilledOrder));

        // Act
        var result = await _controller.FulfillOrder(orderId, fulfillDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OrderDto>(okResult.Value);
        Assert.Equal(OrderStatus.Fulfilled, response.Status);
        Assert.Equal(fulfillDto.TrackingNumber, response.TrackingNumber);
        Assert.NotNull(response.FulfilledAt);
    }

    [Fact]
    public async Task FulfillOrder_WhenOrderNotConfirmed_ReturnsBadRequest()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var fulfillDto = new FulfillOrderDto
        {
            TrackingNumber = "TRACK123456"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<FulfillOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Failure("Order must be confirmed before fulfillment"));

        // Act
        var result = await _controller.FulfillOrder(orderId, fulfillDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Order must be confirmed before fulfillment", badRequestResult.Value?.ToString());
    }

    #endregion

    #region Workflow Integration Tests

    [Fact]
    public async Task OrderWorkflow_CreateConfirmFulfill_WorksCorrectly()
    {
        // This test would verify the complete order workflow
        // In a real scenario, this might be an integration test
        
        // Arrange
        var createDto = new CreateOrderDto
        {
            CustomerId = _testCustomerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100.00m }
            }
        };
        
        var orderId = Guid.NewGuid();
        var createdOrder = new Order { Id = orderId, Status = OrderStatus.Pending };
        var confirmedOrder = new Order { Id = orderId, Status = OrderStatus.Confirmed };
        var fulfilledOrder = new Order { Id = orderId, Status = OrderStatus.Fulfilled };
        
        _mediatorMock.SetupSequence(m => m.Send(It.IsAny<IRequest<Result<Order>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Order>.Success(createdOrder))
                    .ReturnsAsync(Result<Order>.Success(confirmedOrder))
                    .ReturnsAsync(Result<Order>.Success(fulfilledOrder));

        // Act & Assert
        // 1. Create Order
        var createResult = await _controller.CreateOrder(createDto);
        var createdResult = Assert.IsType<CreatedAtActionResult>(createResult);
        
        // 2. Confirm Order
        var confirmResult = await _controller.ConfirmOrder(orderId);
        var confirmOkResult = Assert.IsType<OkObjectResult>(confirmResult);
        
        // 3. Fulfill Order
        var fulfillDto = new FulfillOrderDto { TrackingNumber = "TRACK123" };
        var fulfillResult = await _controller.FulfillOrder(orderId, fulfillDto);
        var fulfillOkResult = Assert.IsType<OkObjectResult>(fulfillResult);
        
        // Verify the workflow completed successfully
        Assert.NotNull(createdResult.Value);
        Assert.NotNull(confirmOkResult.Value);
        Assert.NotNull(fulfillOkResult.Value);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateOrder_WhenExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            CustomerId = _testCustomerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100.00m }
            }
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.CreateOrder(createDto));
        
        // Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}