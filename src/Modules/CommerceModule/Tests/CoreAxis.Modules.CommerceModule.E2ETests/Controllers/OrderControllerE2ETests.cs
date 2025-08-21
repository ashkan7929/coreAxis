using CoreAxis.Modules.CommerceModule.E2ETests.Infrastructure;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using FluentAssertions;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.CommerceModule.E2ETests.Controllers;

public class OrderControllerE2ETests : BaseE2ETest
{
    public OrderControllerE2ETests(CommerceTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldReturnCreatedOrder()
    {
        // Arrange
        var inventoryItems = new List<InventoryItem>();
        for (int i = 0; i < 3; i++)
        {
            inventoryItems.Add(await CreateTestInventoryItemAsync());
        }

        var createDto = CreateValidOrderDto(inventoryItems.Select(x => x.Id).ToList());

        // Act
        var result = await PostAsync<OrderDto>("/api/v1/orders", createDto);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(createDto.CustomerId);
        result.Status.Should().Be(OrderStatus.Pending);
        result.Items.Should().HaveCount(createDto.Items.Count);
        result.TotalAmount.Should().BeGreaterThan(0);

        // Verify in database
        var dbOrder = await DbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == result.Id);
        dbOrder.Should().NotBeNull();
        dbOrder!.Items.Should().HaveCount(createDto.Items.Count);
    }

    [Fact]
    public async Task GetOrder_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);

        // Act
        var result = await GetAsync<OrderDto>($"/api/v1/orders/{order.Id}");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.CustomerId.Should().Be(order.CustomerId);
        result.Status.Should().Be(order.Status);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/v1/orders/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrdersByCustomer_ShouldReturnCustomerOrders()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var inventoryItem = await CreateTestInventoryItemAsync();
        
        var orders = new List<Order>();
        for (int i = 0; i < 3; i++)
        {
            var order = await CreateTestOrderAsync(inventoryItem.Id, customerId);
            orders.Add(order);
        }

        // Act
        var result = await GetAsync<PagedResultDto<OrderDto>>($"/api/v1/orders/customer/{customerId}?page=1&pageSize=10");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(x => x.CustomerId == customerId);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidData_ShouldUpdateStatus()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var updateDto = new UpdateOrderStatusDto
        {
            Status = OrderStatus.Confirmed,
            Notes = "Order confirmed by customer"
        };

        // Act
        var response = await PutAsync($"/api/v1/orders/{order.Id}/status", updateDto);
        response.EnsureSuccessStatusCode();

        // Assert
        var updatedOrder = await GetAsync<OrderDto>($"/api/v1/orders/{order.Id}");
        updatedOrder.Status.Should().Be(OrderStatus.Confirmed);

        // Verify in database
        var dbOrder = await DbContext.Orders.FirstOrDefaultAsync(x => x.Id == order.Id);
        dbOrder!.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task CancelOrder_WithValidId_ShouldCancelOrder()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var cancelDto = new CancelOrderDto
        {
            Reason = "Customer requested cancellation",
            RefundAmount = order.TotalAmount
        };

        // Act
        var response = await PostAsync<object>($"/api/v1/orders/{order.Id}/cancel", cancelDto);

        // Assert
        response.Should().NotBeNull();

        // Verify order is cancelled
        var cancelledOrder = await GetAsync<OrderDto>($"/api/v1/orders/{order.Id}");
        cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task AddOrderItem_WithValidData_ShouldAddItem()
    {
        // Arrange
        var inventoryItem1 = await CreateTestInventoryItemAsync();
        var inventoryItem2 = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem1.Id);
        
        var addItemDto = new AddOrderItemDto
        {
            InventoryItemId = inventoryItem2.Id,
            Quantity = 2,
            UnitPrice = 50m
        };

        // Act
        var response = await PostAsync<object>($"/api/v1/orders/{order.Id}/items", addItemDto);

        // Assert
        response.Should().NotBeNull();

        // Verify item is added
        var updatedOrder = await GetAsync<OrderDto>($"/api/v1/orders/{order.Id}");
        updatedOrder.Items.Should().HaveCount(2);
        updatedOrder.Items.Should().Contain(x => x.InventoryItemId == inventoryItem2.Id);
    }

    [Fact]
    public async Task RemoveOrderItem_WithValidData_ShouldRemoveItem()
    {
        // Arrange
        var inventoryItem1 = await CreateTestInventoryItemAsync();
        var inventoryItem2 = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderWithMultipleItemsAsync(new[] { inventoryItem1.Id, inventoryItem2.Id });
        
        var orderItem = order.Items.First();

        // Act
        var response = await DeleteAsync($"/api/v1/orders/{order.Id}/items/{orderItem.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify item is removed
        var updatedOrder = await GetAsync<OrderDto>($"/api/v1/orders/{order.Id}");
        updatedOrder.Items.Should().HaveCount(1);
        updatedOrder.Items.Should().NotContain(x => x.Id == orderItem.Id);
    }

    [Fact]
    public async Task UpdateOrderItem_WithValidData_ShouldUpdateItem()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var orderItem = order.Items.First();
        
        var updateDto = new UpdateOrderItemDto
        {
            Quantity = 5,
            UnitPrice = 75m
        };

        // Act
        var response = await PutAsync($"/api/v1/orders/{order.Id}/items/{orderItem.Id}", updateDto);
        response.EnsureSuccessStatusCode();

        // Assert
        var updatedOrder = await GetAsync<OrderDto>($"/api/v1/orders/{order.Id}");
        var updatedItem = updatedOrder.Items.First(x => x.Id == orderItem.Id);
        updatedItem.Quantity.Should().Be(5);
        updatedItem.UnitPrice.Should().Be(75m);
    }

    [Fact]
    public async Task GetOrderHistory_ShouldReturnOrderEvents()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        
        // Update order status to create history
        await PutAsync($"/api/v1/orders/{order.Id}/status", new UpdateOrderStatusDto
        {
            Status = OrderStatus.Confirmed,
            Notes = "Order confirmed"
        });

        // Act
        var result = await GetAsync<List<OrderHistoryDto>>($"/api/v1/orders/{order.Id}/history");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(1);
        result.Should().Contain(x => x.Status == OrderStatus.Confirmed);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed, false)]
    public async Task UpdateOrderStatus_WithStatusTransition_ShouldValidateTransition(OrderStatus fromStatus, OrderStatus toStatus, bool shouldSucceed)
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        
        // Set initial status
        if (fromStatus != OrderStatus.Pending)
        {
            await PutAsync($"/api/v1/orders/{order.Id}/status", new UpdateOrderStatusDto
            {
                Status = fromStatus
            });
        }

        var updateDto = new UpdateOrderStatusDto
        {
            Status = toStatus,
            Notes = $"Transition from {fromStatus} to {toStatus}"
        };

        // Act
        var response = await PutAsync($"/api/v1/orders/{order.Id}/status", updateDto);

        // Assert
        if (shouldSucceed)
        {
            response.IsSuccessStatusCode.Should().BeTrue();
        }
        else
        {
            response.IsSuccessStatusCode.Should().BeFalse();
        }
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientInventory_ShouldReturnBadRequest()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var createDto = CreateValidOrderDto(new[] { inventoryItem.Id }.ToList());
        
        // Set quantity higher than available
        createDto.Items.First().Quantity = inventoryItem.Quantity + 10;

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/orders", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = CreateValidOrderDto(new List<Guid>());
        createDto.Items = new List<OrderItemDto>(); // Empty items

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/orders", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Order> CreateTestOrderAsync(Guid inventoryItemId, Guid? customerId = null)
    {
        var customer = customerId ?? Guid.NewGuid();
        var order = new Order(customer);
        
        var orderItem = new OrderItem(inventoryItemId, 2, 50m);
        order.AddItem(orderItem);
        
        await SeedOrderAsync(order);
        return order;
    }

    private async Task<Order> CreateTestOrderWithMultipleItemsAsync(Guid[] inventoryItemIds, Guid? customerId = null)
    {
        var customer = customerId ?? Guid.NewGuid();
        var order = new Order(customer);
        
        foreach (var itemId in inventoryItemIds)
        {
            var orderItem = new OrderItem(itemId, 1, 25m);
            order.AddItem(orderItem);
        }
        
        await SeedOrderAsync(order);
        return order;
    }
}