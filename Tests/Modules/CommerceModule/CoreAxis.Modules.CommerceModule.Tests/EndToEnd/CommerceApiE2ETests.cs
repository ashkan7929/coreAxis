using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace CoreAxis.Modules.CommerceModule.Tests.EndToEnd;

/// <summary>
/// End-to-End tests for Commerce API using real HTTP client
/// </summary>
public class CommerceApiE2ETests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;

    public CommerceApiE2ETests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Configure test-specific services
                services.AddLogging(logging => logging.AddXUnit(output));
            });
        });
        
        _client = _factory.CreateClient();
        _output = output;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    #region Setup and Authentication

    private async Task AuthenticateAsync()
    {
        if (_authToken != null) return;
        
        // Mock authentication - in real scenario, this would call auth endpoint
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };
        
        var loginJson = JsonSerializer.Serialize(loginRequest, _jsonOptions);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        
        // For testing purposes, we'll use a mock token
        _authToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken.Replace("Bearer ", ""));
    }

    private async Task<T?> SendRequestAsync<T>(HttpMethod method, string endpoint, object? content = null)
    {
        await AuthenticateAsync();
        
        var request = new HttpRequestMessage(method, endpoint);
        
        if (content != null)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        var response = await _client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"{method} {endpoint} - Status: {response.StatusCode}");
        _output.WriteLine($"Response: {responseContent}");
        
        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
        {
            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
        }
        
        return default;
    }

    #endregion

    #region Inventory E2E Tests

    [Fact]
    public async Task InventoryWorkflow_CreateUpdateReserveRelease_ShouldWorkEndToEnd()
    {
        // 1. Create inventory item
        var createInventoryDto = new CreateInventoryItemDto
        {
            SKU = $"TEST-SKU-{Guid.NewGuid():N}"[..20],
            Name = "Test Product E2E",
            Description = "Test product for E2E testing",
            Quantity = 100,
            ReservedQuantity = 0,
            Price = 29.99m,
            Cost = 15.00m,
            LowStockThreshold = 10,
            Category = "Electronics",
            Supplier = "Test Supplier",
            Location = "Warehouse A"
        };
        
        var createdItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Post, "/api/v1/inventory", createInventoryDto);
        
        Assert.NotNull(createdItem);
        Assert.Equal(createInventoryDto.SKU, createdItem.SKU);
        Assert.Equal(createInventoryDto.Name, createdItem.Name);
        Assert.Equal(100, createdItem.Quantity);
        
        var itemId = createdItem.Id;
        
        // 2. Get inventory item
        var retrievedItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Get, $"/api/v1/inventory/{itemId}");
        
        Assert.NotNull(retrievedItem);
        Assert.Equal(itemId, retrievedItem.Id);
        Assert.Equal(createInventoryDto.SKU, retrievedItem.SKU);
        
        // 3. Update inventory item
        var updateDto = new UpdateInventoryItemDto
        {
            Name = "Updated Test Product E2E",
            Description = "Updated description",
            Quantity = 150,
            Price = 34.99m,
            LowStockThreshold = 15
        };
        
        var updatedItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Put, 
            $"/api/v1/inventory/{itemId}", updateDto);
        
        Assert.NotNull(updatedItem);
        Assert.Equal(updateDto.Name, updatedItem.Name);
        Assert.Equal(150, updatedItem.Quantity);
        Assert.Equal(34.99m, updatedItem.Price);
        
        // 4. Reserve inventory
        var reserveDto = new ReserveInventoryDto
        {
            Quantity = 25,
            ReservationReference = $"ORDER-{Guid.NewGuid():N}"[..20],
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        
        var reservation = await SendRequestAsync<InventoryReservationDto>(HttpMethod.Post, 
            $"/api/v1/inventory/{itemId}/reserve", reserveDto);
        
        Assert.NotNull(reservation);
        Assert.Equal(25, reservation.Quantity);
        Assert.Equal(reserveDto.ReservationReference, reservation.ReservationReference);
        
        // 5. Verify reservation affected inventory
        var itemAfterReservation = await SendRequestAsync<InventoryItemDto>(HttpMethod.Get, $"/api/v1/inventory/{itemId}");
        
        Assert.NotNull(itemAfterReservation);
        Assert.Equal(25, itemAfterReservation.ReservedQuantity);
        Assert.Equal(125, itemAfterReservation.AvailableQuantity); // 150 - 25
        
        // 6. Release reservation
        var releaseResponse = await _client.DeleteAsync($"/api/v1/inventory/reservations/{reservation.Id}");
        Assert.True(releaseResponse.IsSuccessStatusCode);
        
        // 7. Verify reservation was released
        var itemAfterRelease = await SendRequestAsync<InventoryItemDto>(HttpMethod.Get, $"/api/v1/inventory/{itemId}");
        
        Assert.NotNull(itemAfterRelease);
        Assert.Equal(0, itemAfterRelease.ReservedQuantity);
        Assert.Equal(150, itemAfterRelease.AvailableQuantity);
        
        // 8. Delete inventory item
        var deleteResponse = await _client.DeleteAsync($"/api/v1/inventory/{itemId}");
        Assert.True(deleteResponse.IsSuccessStatusCode);
        
        // 9. Verify item was deleted
        var deletedItemResponse = await _client.GetAsync($"/api/v1/inventory/{itemId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedItemResponse.StatusCode);
    }

    [Fact]
    public async Task InventoryList_WithPaginationAndFiltering_ShouldReturnCorrectResults()
    {
        // Create multiple inventory items for testing
        var items = new List<Guid>();
        
        for (int i = 0; i < 5; i++)
        {
            var createDto = new CreateInventoryItemDto
            {
                SKU = $"FILTER-TEST-{i:D3}",
                Name = $"Filter Test Product {i}",
                Description = "Product for filtering test",
                Quantity = 50 + (i * 10),
                Price = 10.00m + (i * 5),
                Category = i % 2 == 0 ? "Electronics" : "Clothing",
                Supplier = "Test Supplier",
                Location = "Warehouse A"
            };
            
            var createdItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Post, "/api/v1/inventory", createDto);
            if (createdItem != null)
            {
                items.Add(createdItem.Id);
            }
        }
        
        try
        {
            // Test pagination
            var pagedResponse = await SendRequestAsync<PagedResult<InventoryItemDto>>(HttpMethod.Get, 
                "/api/v1/inventory?page=1&pageSize=3");
            
            Assert.NotNull(pagedResponse);
            Assert.True(pagedResponse.Items.Count() <= 3);
            Assert.True(pagedResponse.TotalCount >= 5);
            
            // Test category filtering
            var electronicsResponse = await SendRequestAsync<PagedResult<InventoryItemDto>>(HttpMethod.Get, 
                "/api/v1/inventory?category=Electronics");
            
            Assert.NotNull(electronicsResponse);
            Assert.All(electronicsResponse.Items, item => Assert.Equal("Electronics", item.Category));
            
            // Test search by name
            var searchResponse = await SendRequestAsync<PagedResult<InventoryItemDto>>(HttpMethod.Get, 
                "/api/v1/inventory?search=Filter Test");
            
            Assert.NotNull(searchResponse);
            Assert.All(searchResponse.Items, item => Assert.Contains("Filter Test", item.Name));
        }
        finally
        {
            // Cleanup
            foreach (var itemId in items)
            {
                await _client.DeleteAsync($"/api/v1/inventory/{itemId}");
            }
        }
    }

    #endregion

    #region Order E2E Tests

    [Fact]
    public async Task OrderWorkflow_CreateConfirmFulfill_ShouldWorkEndToEnd()
    {
        // First, create inventory items for the order
        var inventoryItems = new List<Guid>();
        
        for (int i = 0; i < 2; i++)
        {
            var createInventoryDto = new CreateInventoryItemDto
            {
                SKU = $"ORDER-TEST-{i:D3}",
                Name = $"Order Test Product {i}",
                Description = "Product for order testing",
                Quantity = 100,
                Price = 25.00m + (i * 10),
                Category = "Test",
                Supplier = "Test Supplier",
                Location = "Warehouse A"
            };
            
            var createdItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Post, "/api/v1/inventory", createInventoryDto);
            if (createdItem != null)
            {
                inventoryItems.Add(createdItem.Id);
            }
        }
        
        try
        {
            // 1. Create order
            var createOrderDto = new CreateOrderDto
            {
                CustomerId = Guid.NewGuid(),
                Items = inventoryItems.Select((id, index) => new OrderItemDto
                {
                    InventoryItemId = id,
                    Quantity = 2 + index,
                    UnitPrice = 25.00m + (index * 10)
                }).ToList(),
                ShippingAddress = new AddressDto
                {
                    Street = "123 Test Street",
                    City = "Test City",
                    State = "Test State",
                    PostalCode = "12345",
                    Country = "Test Country"
                },
                BillingAddress = new AddressDto
                {
                    Street = "123 Test Street",
                    City = "Test City",
                    State = "Test State",
                    PostalCode = "12345",
                    Country = "Test Country"
                }
            };
            
            var createdOrder = await SendRequestAsync<OrderDto>(HttpMethod.Post, "/api/v1/orders", createOrderDto);
            
            Assert.NotNull(createdOrder);
            Assert.Equal(OrderStatus.Pending, createdOrder.Status);
            Assert.Equal(2, createdOrder.Items.Count());
            
            var orderId = createdOrder.Id;
            
            // 2. Get order
            var retrievedOrder = await SendRequestAsync<OrderDto>(HttpMethod.Get, $"/api/v1/orders/{orderId}");
            
            Assert.NotNull(retrievedOrder);
            Assert.Equal(orderId, retrievedOrder.Id);
            Assert.Equal(OrderStatus.Pending, retrievedOrder.Status);
            
            // 3. Confirm order
            var confirmResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/confirm", null);
            Assert.True(confirmResponse.IsSuccessStatusCode);
            
            var confirmedOrderJson = await confirmResponse.Content.ReadAsStringAsync();
            var confirmedOrder = JsonSerializer.Deserialize<OrderDto>(confirmedOrderJson, _jsonOptions);
            
            Assert.NotNull(confirmedOrder);
            Assert.Equal(OrderStatus.Confirmed, confirmedOrder.Status);
            
            // 4. Fulfill order
            var fulfillResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/fulfill", null);
            Assert.True(fulfillResponse.IsSuccessStatusCode);
            
            var fulfilledOrderJson = await fulfillResponse.Content.ReadAsStringAsync();
            var fulfilledOrder = JsonSerializer.Deserialize<OrderDto>(fulfilledOrderJson, _jsonOptions);
            
            Assert.NotNull(fulfilledOrder);
            Assert.Equal(OrderStatus.Fulfilled, fulfilledOrder.Status);
            Assert.NotNull(fulfilledOrder.FulfilledAt);
            
            // 5. Verify inventory was updated
            foreach (var inventoryId in inventoryItems)
            {
                var inventoryItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Get, $"/api/v1/inventory/{inventoryId}");
                Assert.NotNull(inventoryItem);
                // Verify quantity was reduced (exact amount depends on order items)
                Assert.True(inventoryItem.Quantity < 100);
            }
        }
        finally
        {
            // Cleanup inventory items
            foreach (var itemId in inventoryItems)
            {
                await _client.DeleteAsync($"/api/v1/inventory/{itemId}");
            }
        }
    }

    [Fact]
    public async Task OrderCancellation_ShouldReleaseInventoryReservations()
    {
        // Create inventory item
        var createInventoryDto = new CreateInventoryItemDto
        {
            SKU = "CANCEL-TEST-001",
            Name = "Cancellation Test Product",
            Description = "Product for cancellation testing",
            Quantity = 50,
            Price = 30.00m,
            Category = "Test",
            Supplier = "Test Supplier",
            Location = "Warehouse A"
        };
        
        var inventoryItem = await SendRequestAsync<InventoryItemDto>(HttpMethod.Post, "/api/v1/inventory", createInventoryDto);
        Assert.NotNull(inventoryItem);
        
        try
        {
            // Create order
            var createOrderDto = new CreateOrderDto
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<OrderItemDto>
                {
                    new()
                    {
                        InventoryItemId = inventoryItem.Id,
                        Quantity = 10,
                        UnitPrice = 30.00m
                    }
                },
                ShippingAddress = new AddressDto
                {
                    Street = "123 Test Street",
                    City = "Test City",
                    State = "Test State",
                    PostalCode = "12345",
                    Country = "Test Country"
                }
            };
            
            var order = await SendRequestAsync<OrderDto>(HttpMethod.Post, "/api/v1/orders", createOrderDto);
            Assert.NotNull(order);
            
            // Verify inventory was reserved
            var reservedInventory = await SendRequestAsync<InventoryItemDto>(HttpMethod.Get, $"/api/v1/inventory/{inventoryItem.Id}");
            Assert.NotNull(reservedInventory);
            Assert.Equal(10, reservedInventory.ReservedQuantity);
            
            // Cancel order
            var cancelDto = new CancelOrderDto
            {
                Reason = "Customer requested cancellation"
            };
            
            var cancelledOrder = await SendRequestAsync<OrderDto>(HttpMethod.Post, 
                $"/api/v1/orders/{order.Id}/cancel", cancelDto);
            
            Assert.NotNull(cancelledOrder);
            Assert.Equal(OrderStatus.Cancelled, cancelledOrder.Status);
            
            // Verify inventory reservation was released
            var releasedInventory = await SendRequestAsync<InventoryItemDto>(HttpMethod.Get, $"/api/v1/inventory/{inventoryItem.Id}");
            Assert.NotNull(releasedInventory);
            Assert.Equal(0, releasedInventory.ReservedQuantity);
            Assert.Equal(50, releasedInventory.AvailableQuantity);
        }
        finally
        {
            // Cleanup
            await _client.DeleteAsync($"/api/v1/inventory/{inventoryItem.Id}");
        }
    }

    #endregion

    #region Payment E2E Tests

    [Fact]
    public async Task PaymentWorkflow_ProcessAndRefund_ShouldWorkEndToEnd()
    {
        // Create an order first (simplified for payment testing)
        var orderId = Guid.NewGuid();
        
        // 1. Process payment
        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = orderId,
            Amount = 99.99m,
            PaymentMethod = "CreditCard",
            PaymentDetails = new Dictionary<string, object>
            {
                { "cardNumber", "**** **** **** 1234" },
                { "expiryMonth", "12" },
                { "expiryYear", "2025" },
                { "cvv", "***" }
            },
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        
        var payment = await SendRequestAsync<PaymentDto>(HttpMethod.Post, "/api/v1/payments", processPaymentDto);
        
        Assert.NotNull(payment);
        Assert.Equal(orderId, payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.NotNull(payment.TransactionId);
        
        var paymentId = payment.Id;
        
        // 2. Get payment
        var retrievedPayment = await SendRequestAsync<PaymentDto>(HttpMethod.Get, $"/api/v1/payments/{paymentId}");
        
        Assert.NotNull(retrievedPayment);
        Assert.Equal(paymentId, retrievedPayment.Id);
        Assert.Equal(PaymentStatus.Completed, retrievedPayment.Status);
        
        // 3. Verify payment status
        var verifiedPayment = await SendRequestAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/payments/{paymentId}/verify");
        
        Assert.NotNull(verifiedPayment);
        Assert.Equal(PaymentStatus.Completed, verifiedPayment.Status);
        
        // 4. Process partial refund
        var refundDto = new ProcessRefundDto
        {
            Amount = 25.00m,
            Reason = "Partial refund for damaged item"
        };
        
        var refund = await SendRequestAsync<RefundDto>(HttpMethod.Post, 
            $"/api/v1/payments/{paymentId}/refunds", refundDto);
        
        Assert.NotNull(refund);
        Assert.Equal(paymentId, refund.PaymentId);
        Assert.Equal(25.00m, refund.Amount);
        Assert.Equal(RefundStatus.Completed, refund.Status);
        
        // 5. Get refunds for payment
        var refunds = await SendRequestAsync<List<RefundDto>>(HttpMethod.Get, $"/api/v1/payments/{paymentId}/refunds");
        
        Assert.NotNull(refunds);
        Assert.Single(refunds);
        Assert.Equal(25.00m, refunds.First().Amount);
        
        // 6. Process another refund (should work for remaining amount)
        var secondRefundDto = new ProcessRefundDto
        {
            Amount = 74.99m,
            Reason = "Full refund for remaining amount"
        };
        
        var secondRefund = await SendRequestAsync<RefundDto>(HttpMethod.Post, 
            $"/api/v1/payments/{paymentId}/refunds", secondRefundDto);
        
        Assert.NotNull(secondRefund);
        Assert.Equal(74.99m, secondRefund.Amount);
        
        // 7. Verify total refunds
        var allRefunds = await SendRequestAsync<List<RefundDto>>(HttpMethod.Get, $"/api/v1/payments/{paymentId}/refunds");
        
        Assert.NotNull(allRefunds);
        Assert.Equal(2, allRefunds.Count);
        Assert.Equal(99.99m, allRefunds.Sum(r => r.Amount));
    }

    #endregion

    #region Subscription E2E Tests

    [Fact]
    public async Task SubscriptionLifecycle_CreatePauseResumeCancel_ShouldWorkEndToEnd()
    {
        var planId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        
        // 1. Create subscription
        var createSubscriptionDto = new CreateSubscriptionDto
        {
            PlanId = planId,
            StartDate = DateTime.UtcNow.AddDays(1),
            PaymentMethodId = paymentMethodId,
            PromoCode = "WELCOME10"
        };
        
        var subscription = await SendRequestAsync<SubscriptionDto>(HttpMethod.Post, "/api/v1/subscriptions", createSubscriptionDto);
        
        Assert.NotNull(subscription);
        Assert.Equal(planId, subscription.PlanId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        
        var subscriptionId = subscription.Id;
        
        // 2. Get subscription
        var retrievedSubscription = await SendRequestAsync<SubscriptionDto>(HttpMethod.Get, $"/api/v1/subscriptions/{subscriptionId}");
        
        Assert.NotNull(retrievedSubscription);
        Assert.Equal(subscriptionId, retrievedSubscription.Id);
        Assert.Equal(SubscriptionStatus.Active, retrievedSubscription.Status);
        
        // 3. Update subscription
        var updateDto = new UpdateSubscriptionDto
        {
            PlanId = Guid.NewGuid(), // Upgrade to different plan
            PaymentMethodId = paymentMethodId
        };
        
        var updatedSubscription = await SendRequestAsync<SubscriptionDto>(HttpMethod.Put, 
            $"/api/v1/subscriptions/{subscriptionId}", updateDto);
        
        Assert.NotNull(updatedSubscription);
        Assert.Equal(updateDto.PlanId, updatedSubscription.PlanId);
        
        // 4. Pause subscription
        var pauseDto = new PauseSubscriptionDto
        {
            Reason = "Temporary pause for vacation",
            PauseDuration = TimeSpan.FromDays(30)
        };
        
        var pausedSubscription = await SendRequestAsync<SubscriptionDto>(HttpMethod.Post, 
            $"/api/v1/subscriptions/{subscriptionId}/pause", pauseDto);
        
        Assert.NotNull(pausedSubscription);
        Assert.Equal(SubscriptionStatus.Paused, pausedSubscription.Status);
        Assert.Equal(pauseDto.Reason, pausedSubscription.PauseReason);
        
        // 5. Resume subscription
        var resumeResponse = await _client.PostAsync($"/api/v1/subscriptions/{subscriptionId}/resume", null);
        Assert.True(resumeResponse.IsSuccessStatusCode);
        
        var resumedSubscriptionJson = await resumeResponse.Content.ReadAsStringAsync();
        var resumedSubscription = JsonSerializer.Deserialize<SubscriptionDto>(resumedSubscriptionJson, _jsonOptions);
        
        Assert.NotNull(resumedSubscription);
        Assert.Equal(SubscriptionStatus.Active, resumedSubscription.Status);
        Assert.Null(resumedSubscription.PauseReason);
        
        // 6. Cancel subscription
        var cancelDto = new CancelSubscriptionDto
        {
            Reason = "No longer needed",
            CancelImmediately = false
        };
        
        var cancelledSubscription = await SendRequestAsync<SubscriptionDto>(HttpMethod.Post, 
            $"/api/v1/subscriptions/{subscriptionId}/cancel", cancelDto);
        
        Assert.NotNull(cancelledSubscription);
        Assert.Equal(SubscriptionStatus.Cancelled, cancelledSubscription.Status);
        Assert.Equal(cancelDto.Reason, cancelledSubscription.CancellationReason);
        Assert.NotNull(cancelledSubscription.CancelledAt);
    }

    [Fact]
    public async Task SubscriptionList_WithFiltering_ShouldReturnCorrectResults()
    {
        var subscriptions = new List<Guid>();
        
        // Create multiple subscriptions for testing
        for (int i = 0; i < 3; i++)
        {
            var createDto = new CreateSubscriptionDto
            {
                PlanId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(i),
                PaymentMethodId = Guid.NewGuid()
            };
            
            var subscription = await SendRequestAsync<SubscriptionDto>(HttpMethod.Post, "/api/v1/subscriptions", createDto);
            if (subscription != null)
            {
                subscriptions.Add(subscription.Id);
            }
        }
        
        try
        {
            // Test getting all subscriptions
            var allSubscriptions = await SendRequestAsync<PagedResult<SubscriptionDto>>(HttpMethod.Get, 
                "/api/v1/subscriptions?page=1&pageSize=10");
            
            Assert.NotNull(allSubscriptions);
            Assert.True(allSubscriptions.TotalCount >= 3);
            
            // Test filtering by status
            var activeSubscriptions = await SendRequestAsync<PagedResult<SubscriptionDto>>(HttpMethod.Get, 
                "/api/v1/subscriptions?status=Active");
            
            Assert.NotNull(activeSubscriptions);
            Assert.All(activeSubscriptions.Items, s => Assert.Equal(SubscriptionStatus.Active, s.Status));
        }
        finally
        {
            // Cleanup - cancel all test subscriptions
            foreach (var subscriptionId in subscriptions)
            {
                var cancelDto = new CancelSubscriptionDto
                {
                    Reason = "Test cleanup",
                    CancelImmediately = true
                };
                
                await SendRequestAsync<SubscriptionDto>(HttpMethod.Post, 
                    $"/api/v1/subscriptions/{subscriptionId}/cancel", cancelDto);
            }
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ApiEndpoints_WithInvalidData_ShouldReturnBadRequest()
    {
        await AuthenticateAsync();
        
        // Test invalid inventory creation
        var invalidInventoryDto = new CreateInventoryItemDto
        {
            SKU = "", // Invalid empty SKU
            Name = "", // Invalid empty name
            Quantity = -1, // Invalid negative quantity
            Price = -10.00m // Invalid negative price
        };
        
        var inventoryResponse = await _client.PostAsync("/api/v1/inventory", 
            new StringContent(JsonSerializer.Serialize(invalidInventoryDto, _jsonOptions), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.BadRequest, inventoryResponse.StatusCode);
        
        // Test invalid payment processing
        var invalidPaymentDto = new ProcessPaymentDto
        {
            OrderId = Guid.Empty, // Invalid empty GUID
            Amount = -100.00m, // Invalid negative amount
            PaymentMethod = "" // Invalid empty payment method
        };
        
        var paymentResponse = await _client.PostAsync("/api/v1/payments", 
            new StringContent(JsonSerializer.Serialize(invalidPaymentDto, _jsonOptions), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.BadRequest, paymentResponse.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_WithNonExistentIds_ShouldReturnNotFound()
    {
        await AuthenticateAsync();
        
        var nonExistentId = Guid.NewGuid();
        
        // Test non-existent inventory item
        var inventoryResponse = await _client.GetAsync($"/api/v1/inventory/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, inventoryResponse.StatusCode);
        
        // Test non-existent order
        var orderResponse = await _client.GetAsync($"/api/v1/orders/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, orderResponse.StatusCode);
        
        // Test non-existent payment
        var paymentResponse = await _client.GetAsync($"/api/v1/payments/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, paymentResponse.StatusCode);
        
        // Test non-existent subscription
        var subscriptionResponse = await _client.GetAsync($"/api/v1/subscriptions/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, subscriptionResponse.StatusCode);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ApiEndpoints_UnderLoad_ShouldMaintainPerformance()
    {
        await AuthenticateAsync();
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();
        
        // Simulate concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var response = await _client.GetAsync("/api/v1/inventory?page=1&pageSize=10");
                Assert.True(response.IsSuccessStatusCode);
            }));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // All 10 concurrent requests should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Concurrent requests took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        
        _output.WriteLine($"10 concurrent requests completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// Helper class for paginated results
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Helper DTOs for testing
/// </summary>
public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}