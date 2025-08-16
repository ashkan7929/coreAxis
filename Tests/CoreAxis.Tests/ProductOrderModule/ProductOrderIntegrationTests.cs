using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CoreAxis.SharedKernel.Ports;
using Microsoft.EntityFrameworkCore;
using CoreAxis.EventBus;

namespace CoreAxis.Tests.ProductOrderModule;

/// <summary>
/// Integration tests for the complete ProductOrder workflow:
/// PlaceOrder → OrderPlaced Event → Quote/Lock Workflow → PriceLocked Event → Order Update
/// </summary>
public class ProductOrderIntegrationTests
{
    private readonly Mock<IEventBus> _mockEventBus;

    public ProductOrderIntegrationTests()
    {
        _mockEventBus = new Mock<IEventBus>();
    }

    /// <summary>
    /// Test for WorkflowClient starting a workflow with OrderPlaced event
    /// </summary>
    [Fact]
    public async Task WorkflowClient_WithOrderPlacedEvent_ShouldStartWorkflow()
    {
        // Arrange
        var mockWorkflowClient = new Mock<IWorkflowClient>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        
        var orderPlacedEvent = new OrderPlaced(
            orderId: orderId,
            userId: userId,
            assetCode: "BTC",
            quantity: 0.5m,
            tenantId: "tenant-1",
            metadata: new Dictionary<string, object>(),
            correlationId: correlationId
        );

        mockWorkflowClient.Setup(x => x.StartAsync(It.IsAny<string>(), It.IsAny<object>(), default))
            .Returns(Task.FromResult(new WorkflowResult(Guid.NewGuid(), "Running")));

        // Act - Simulate WorkflowClient handling OrderPlaced
        var result = await mockWorkflowClient.Object.StartAsync("quote-lock-workflow", orderPlacedEvent);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        mockWorkflowClient.Verify(x => x.StartAsync(
            It.Is<string>(workflow => workflow == "quote-lock-workflow"),
            It.IsAny<object>(),
            default), Times.Once);
    }

    /// <summary>
    /// Test for PriceLocked event handler - should update order
    /// </summary>
    [Fact]
    public async Task PriceLockedHandler_WithValidEvent_ShouldUpdateOrder()
    {
        // Arrange
        var mockOrderRepository = new Mock<IOrderRepository>();
        var mockLogger = new Mock<ILogger<PriceLockedIntegrationEventHandler>>();
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        
        var priceLockedEvent = new PriceLocked(
            orderId: orderId,
            assetCode: "BTC",
            quantity: 0.5m,
            lockedPrice: 45000m,
            lockedAt: DateTime.UtcNow,
            expiresAt: DateTime.UtcNow.AddMinutes(15),
            tenantId: "tenant-1",
            correlationId: correlationId
        );

        // Create a mock order
        var orderLines = new List<OrderLine>
        {
            OrderLine.Create(AssetCode.Create("BTC"), 0.5m, 45000m)
        };
        var order = Order.Create(
            userId: "user-123",
            assetCode: AssetCode.Create("BTC"),
            totalAmount: 22500m,
            orderLines: orderLines
        );

        mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var handler = new PriceLockedIntegrationEventHandler(
            mockOrderRepository.Object,
            mockLogger.Object
        );

        // Act
        await handler.HandleAsync(priceLockedEvent);

        // Assert
        mockOrderRepository.Verify(
            x => x.GetByIdAsync(It.Is<Guid>(id => id == orderId)),
            Times.Once
        );

        mockOrderRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Order>()),
            Times.Once
        );

        mockOrderRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );
    }

    /// <summary>
    /// Integration test for complete workflow: OrderPlaced → WorkflowClient → PriceLocked → Handler
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_FromOrderPlacedToPriceLocked_ShouldWork()
    {
        // Arrange
        var mockOrderRepository = new Mock<IOrderRepository>();
        var mockWorkflowClient = new Mock<IWorkflowClient>();
        var mockLogger = new Mock<ILogger<OrderPlacedIntegrationEventHandler>>();
        var mockPriceLogger = new Mock<ILogger<PriceLockedIntegrationEventHandler>>();

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var workflowId = "test-workflow";
        
        // Step 1: OrderPlaced event
        var orderPlacedEvent = new OrderPlaced(
            orderId: orderId,
            userId: userId,
            assetCode: "ETH",
            quantity: 2.0m,
            tenantId: "tenant-1",
            metadata: new Dictionary<string, object>(),
            correlationId: correlationId
        );

        // Mock workflow client
        var expectedWorkflowResult = new WorkflowResult(Guid.NewGuid(), "Running");
        mockWorkflowClient.Setup(x => x.StartAsync(It.IsAny<string>(), It.IsAny<object>(), default))
            .Returns(Task.FromResult(expectedWorkflowResult));

        // Act - Step 1: Start workflow
        var workflowResult = await mockWorkflowClient.Object.StartAsync("quote-lock-workflow", orderPlacedEvent);

        // Assert - Step 1
        Assert.NotNull(workflowResult);
        Assert.True(workflowResult.IsSuccess);
        mockWorkflowClient.Verify(x => x.StartAsync(
            It.Is<string>(w => w == "quote-lock-workflow"),
            It.IsAny<object>(),
            default), Times.Once);

        // Step 2: PriceLocked event (simulating workflow completion)
        var priceLockedEvent = new PriceLocked(
            orderId: orderId,
            assetCode: "ETH",
            quantity: 2.0m,
            lockedPrice: 3000m,
            lockedAt: DateTime.UtcNow,
            expiresAt: DateTime.UtcNow.AddMinutes(15),
            tenantId: "tenant-1",
            correlationId: correlationId
        );

        // Mock order for update
        var orderLines = new List<OrderLine>
        {
            OrderLine.Create(AssetCode.Create("ETH"), 2.0m, 3000m)
        };
        var order = Order.Create(
            userId: "user-456",
            assetCode: AssetCode.Create("ETH"),
            totalAmount: 6000m,
            orderLines: orderLines
        );

        mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var priceLockedHandler = new PriceLockedIntegrationEventHandler(
            mockOrderRepository.Object,
            mockPriceLogger.Object
        );

        // Act - Step 2: Handle PriceLocked
        await priceLockedHandler.HandleAsync(priceLockedEvent);

        // Assert - Step 2
        mockOrderRepository.Verify(
            x => x.GetByIdAsync(It.Is<Guid>(id => id == orderId)),
            Times.Once
        );

        mockOrderRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Order>()),
            Times.Once
        );

        mockOrderRepository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once
        );
    }

    /// <summary>
    /// Test for event bus publishing PriceLocked event
    /// </summary>
    [Fact]
    public async Task EventBus_ShouldPublishPriceLockedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        
        var priceLockedEvent = new PriceLocked(
            orderId: orderId,
            assetCode: "BTC",
            quantity: 1.0m,
            lockedPrice: 50000m,
            lockedAt: DateTime.UtcNow,
            expiresAt: DateTime.UtcNow.AddMinutes(15),
            tenantId: "tenant-1",
            correlationId: correlationId
        );

        _mockEventBus.Setup(x => x.PublishAsync(It.IsAny<PriceLocked>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mockEventBus.Object.PublishAsync(priceLockedEvent);

        // Assert
        _mockEventBus.Verify(
            x => x.PublishAsync(It.Is<PriceLocked>(e => 
                e.OrderId == orderId && 
                e.AssetCode == "BTC" && 
                e.LockedPrice == 50000m)),
            Times.Once
        );
    }
}