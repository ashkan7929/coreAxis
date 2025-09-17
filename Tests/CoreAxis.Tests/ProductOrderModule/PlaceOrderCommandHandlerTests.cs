using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ProductOrderModule.Application.Commands;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Application.Handlers;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreAxis.Tests.ProductOrderModule;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ILogger<PlaceOrderCommandHandler>> _mockLogger;
    private readonly PlaceOrderCommandHandler _handler;

    public PlaceOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<PlaceOrderCommandHandler>>();
        _handler = new PlaceOrderCommandHandler(_mockOrderRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithNewIdempotencyKey_ShouldCreateNewOrder()
    {
        // Arrange
        var userId = "user123";
        var idempotencyKey = "unique-key-123";
        var command = new PlaceOrderCommand
        {
            UserId = userId,
            AssetCode = "BTC",
            TotalAmount = 1000m,
            IdempotencyKey = idempotencyKey,
            OrderLines = new List<PlaceOrderLineDto>
            {
                new PlaceOrderLineDto
                {
                    AssetCode = "BTC",
                    Quantity = 0.5m,
                    UnitPrice = 2000m
                }
            }
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey))
            .ReturnsAsync((Order?)null);

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        _mockOrderRepository.Verify(r => r.GetByIdempotencyKeyAsync(idempotencyKey), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingIdempotencyKey_ShouldReturnExistingOrder()
    {
        // Arrange
        var userId = "user123";
        var idempotencyKey = "existing-key-123";
        var command = new PlaceOrderCommand
        {
            UserId = userId,
            AssetCode = "BTC",
            TotalAmount = 1000m,
            IdempotencyKey = idempotencyKey,
            OrderLines = new List<PlaceOrderLineDto>
            {
                new PlaceOrderLineDto
                {
                    AssetCode = "BTC",
                    Quantity = 0.5m,
                    UnitPrice = 2000m
                }
            }
        };

        var orderLines = new List<OrderLine>
        {
            OrderLine.Create("BTC", 0.5m, 2000m)
        };
        var existingOrder = Order.Create(
            userId: userId,
            assetCode: AssetCode.Create("BTC"),
            totalAmount: 1000m,
            orderLines: orderLines
        );
        existingOrder.SetIdempotencyKey(idempotencyKey);

        _mockOrderRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingOrder.Id, result.Id);
        Assert.Equal(userId, result.UserId);
        _mockOrderRepository.Verify(r => r.GetByIdempotencyKeyAsync(idempotencyKey), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullIdempotencyKey_ShouldCreateNewOrderWithoutIdempotencyCheck()
    {
        // Arrange
        var userId = "user123";
        var command = new PlaceOrderCommand
        {
            UserId = userId,
            AssetCode = "BTC",
            TotalAmount = 1000m,
            IdempotencyKey = null,
            OrderLines = new List<PlaceOrderLineDto>
            {
                new PlaceOrderLineDto
                {
                    AssetCode = "BTC",
                    Quantity = 0.5m,
                    UnitPrice = 2000m
                }
            }
        };

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        _mockOrderRepository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyIdempotencyKey_ShouldCreateNewOrderWithoutIdempotencyCheck()
    {
        // Arrange
        var userId = "user123";
        var command = new PlaceOrderCommand
        {
            UserId = userId,
            AssetCode = "BTC",
            TotalAmount = 1000m,
            IdempotencyKey = string.Empty,
            OrderLines = new List<PlaceOrderLineDto>
            {
                new PlaceOrderLineDto
                {
                    AssetCode = "BTC",
                    Quantity = 0.5m,
                    UnitPrice = 2000m
                }
            }
        };

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        _mockOrderRepository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }
}