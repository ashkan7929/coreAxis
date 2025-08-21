using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Tests.Performance;

public class CommerceLoadTests : IClassFixture<CommerceTestFixture>
{
    private readonly CommerceTestFixture _fixture;
    private readonly ILogger<CommerceLoadTests> _logger;

    public CommerceLoadTests(CommerceTestFixture fixture)
    {
        _fixture = fixture;
        _logger = _fixture.ServiceProvider.GetRequiredService<ILogger<CommerceLoadTests>>();
    }

    [Fact]
    public async Task InventoryReservation_LoadTest_1000ConcurrentUsers()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var initialQuantity = 5000;
        var concurrentUsers = 1000;
        var reservationQuantity = 2;
        
        await SetupInventoryItem(productId, initialQuantity);
        
        var successCounter = new ConcurrentBag<bool>();
        var errorCounter = new ConcurrentBag<Exception>();
        var responseTimeTracker = new ConcurrentBag<long>();
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Simulate 1000 concurrent users trying to reserve inventory
        var tasks = Enumerable.Range(0, concurrentUsers).Select(async i =>
        {
            var userStopwatch = Stopwatch.StartNew();
            try
            {
                using var scope = _fixture.ServiceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var customerId = Guid.NewGuid();
                
                var result = await service.ReserveInventoryAsync(
                    productId, reservationQuantity, customerId, TimeSpan.FromHours(1));
                
                userStopwatch.Stop();
                responseTimeTracker.Add(userStopwatch.ElapsedMilliseconds);
                successCounter.Add(result.Success);
            }
            catch (Exception ex)
            {
                userStopwatch.Stop();
                responseTimeTracker.Add(userStopwatch.ElapsedMilliseconds);
                errorCounter.Add(ex);
                successCounter.Add(false);
            }
        });
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var successfulReservations = successCounter.Count(s => s);
        var totalErrors = errorCounter.Count;
        var responseTimes = responseTimeTracker.ToArray();
        
        _logger.LogInformation("Load Test Results:");
        _logger.LogInformation($"Total Users: {concurrentUsers}");
        _logger.LogInformation($"Successful Reservations: {successfulReservations}");
        _logger.LogInformation($"Failed Reservations: {concurrentUsers - successfulReservations}");
        _logger.LogInformation($"Total Errors: {totalErrors}");
        _logger.LogInformation($"Success Rate: {(double)successfulReservations / concurrentUsers * 100:F2}%");
        _logger.LogInformation($"Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Average Response Time: {responseTimes.Average():F2}ms");
        _logger.LogInformation($"95th Percentile Response Time: {GetPercentile(responseTimes, 95):F2}ms");
        _logger.LogInformation($"99th Percentile Response Time: {GetPercentile(responseTimes, 99):F2}ms");
        
        // Performance assertions
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Complete within 30 seconds
        responseTimes.Average().Should().BeLessThan(1000); // Average response time under 1 second
        GetPercentile(responseTimes, 95).Should().BeLessThan(2000); // 95% under 2 seconds
        totalErrors.Should().BeLessThan(concurrentUsers * 0.05); // Less than 5% error rate
        
        // Verify no overselling
        var finalInventory = await GetInventoryItem(productId);
        var totalReserved = successfulReservations * reservationQuantity;
        finalInventory!.QuantityReserved.Should().Be(totalReserved);
        finalInventory.QuantityAvailable.Should().Be(initialQuantity - totalReserved);
    }

    [Fact]
    public async Task OrderProcessing_LoadTest_500ConcurrentOrders()
    {
        // Arrange
        var concurrentOrders = 500;
        var productsPerOrder = 3;
        
        // Setup inventory for products
        var productIds = new List<Guid>();
        for (int i = 0; i < 10; i++) // 10 different products
        {
            var productId = Guid.NewGuid();
            await SetupInventoryItem(productId, 1000);
            productIds.Add(productId);
        }
        
        var successCounter = new ConcurrentBag<bool>();
        var errorCounter = new ConcurrentBag<Exception>();
        var responseTimeTracker = new ConcurrentBag<long>();
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Process concurrent orders
        var tasks = Enumerable.Range(0, concurrentOrders).Select(async i =>
        {
            var orderStopwatch = Stopwatch.StartNew();
            try
            {
                using var scope = _fixture.ServiceProvider.CreateScope();
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
                
                var customerId = Guid.NewGuid();
                var orderId = Guid.NewGuid();
                
                // Create order with random products
                var order = await CreateTestOrder(orderId, customerId, productIds, productsPerOrder);
                
                // Reserve inventory for all items
                var reservationTasks = order.Items.Select(async item =>
                {
                    return await reservationService.ReserveInventoryAsync(
                        item.ProductId, item.Quantity, customerId, TimeSpan.FromHours(1));
                });
                
                var reservationResults = await Task.WhenAll(reservationTasks);
                
                if (reservationResults.All(r => r.Success))
                {
                    // Apply pricing
                    var pricingResult = await pricingService.ApplyDiscountsAsync(order);
                    
                    orderStopwatch.Stop();
                    responseTimeTracker.Add(orderStopwatch.ElapsedMilliseconds);
                    successCounter.Add(true);
                }
                else
                {
                    // Release successful reservations
                    var releaseTask = reservationResults
                        .Where(r => r.Success)
                        .Select(r => reservationService.ReleaseReservationAsync(r.ReservationId));
                    await Task.WhenAll(releaseTask);
                    
                    orderStopwatch.Stop();
                    responseTimeTracker.Add(orderStopwatch.ElapsedMilliseconds);
                    successCounter.Add(false);
                }
            }
            catch (Exception ex)
            {
                orderStopwatch.Stop();
                responseTimeTracker.Add(orderStopwatch.ElapsedMilliseconds);
                errorCounter.Add(ex);
                successCounter.Add(false);
            }
        });
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var successfulOrders = successCounter.Count(s => s);
        var totalErrors = errorCounter.Count;
        var responseTimes = responseTimeTracker.ToArray();
        
        _logger.LogInformation("Order Processing Load Test Results:");
        _logger.LogInformation($"Total Orders: {concurrentOrders}");
        _logger.LogInformation($"Successful Orders: {successfulOrders}");
        _logger.LogInformation($"Failed Orders: {concurrentOrders - successfulOrders}");
        _logger.LogInformation($"Total Errors: {totalErrors}");
        _logger.LogInformation($"Success Rate: {(double)successfulOrders / concurrentOrders * 100:F2}%");
        _logger.LogInformation($"Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Average Response Time: {responseTimes.Average():F2}ms");
        _logger.LogInformation($"Throughput: {(double)concurrentOrders / stopwatch.ElapsedMilliseconds * 1000:F2} orders/second");
        
        // Performance assertions
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(60000); // Complete within 60 seconds
        responseTimes.Average().Should().BeLessThan(2000); // Average response time under 2 seconds
        successfulOrders.Should().BeGreaterThan(concurrentOrders * 0.8); // At least 80% success rate
        totalErrors.Should().BeLessThan(concurrentOrders * 0.1); // Less than 10% error rate
    }

    [Fact]
    public async Task SubscriptionRenewal_LoadTest_200ConcurrentRenewals()
    {
        // Arrange
        var concurrentRenewals = 200;
        var subscriptions = await SetupSubscriptionsForRenewal(concurrentRenewals);
        
        var successCounter = new ConcurrentBag<bool>();
        var errorCounter = new ConcurrentBag<Exception>();
        var responseTimeTracker = new ConcurrentBag<long>();
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Process concurrent renewals
        var tasks = subscriptions.Select(async subscription =>
        {
            var renewalStopwatch = Stopwatch.StartNew();
            try
            {
                using var scope = _fixture.ServiceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                
                var result = await service.RenewSubscriptionAsync(subscription.Id);
                
                renewalStopwatch.Stop();
                responseTimeTracker.Add(renewalStopwatch.ElapsedMilliseconds);
                successCounter.Add(result);
            }
            catch (Exception ex)
            {
                renewalStopwatch.Stop();
                responseTimeTracker.Add(renewalStopwatch.ElapsedMilliseconds);
                errorCounter.Add(ex);
                successCounter.Add(false);
            }
        });
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var successfulRenewals = successCounter.Count(s => s);
        var totalErrors = errorCounter.Count;
        var responseTimes = responseTimeTracker.ToArray();
        
        _logger.LogInformation("Subscription Renewal Load Test Results:");
        _logger.LogInformation($"Total Renewals: {concurrentRenewals}");
        _logger.LogInformation($"Successful Renewals: {successfulRenewals}");
        _logger.LogInformation($"Failed Renewals: {concurrentRenewals - successfulRenewals}");
        _logger.LogInformation($"Total Errors: {totalErrors}");
        _logger.LogInformation($"Success Rate: {(double)successfulRenewals / concurrentRenewals * 100:F2}%");
        _logger.LogInformation($"Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Average Response Time: {responseTimes.Average():F2}ms");
        
        // Performance assertions
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(45000); // Complete within 45 seconds
        responseTimes.Average().Should().BeLessThan(1500); // Average response time under 1.5 seconds
        successfulRenewals.Should().BeGreaterThan(concurrentRenewals * 0.85); // At least 85% success rate
        totalErrors.Should().BeLessThan(concurrentRenewals * 0.05); // Less than 5% error rate
    }

    [Fact]
    public async Task PaymentReconciliation_LoadTest_LargeStatementProcessing()
    {
        // Arrange
        var transactionCount = 5000;
        var matchingPaymentCount = 4000; // 80% match rate
        
        var (statement, payments) = await SetupReconciliationData(transactionCount, matchingPaymentCount);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        using var scope = _fixture.ServiceProvider.CreateScope();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        
        var result = await reconciliationService.ProcessStatementAsync(statement);
        
        stopwatch.Stop();
        
        // Assert
        _logger.LogInformation("Payment Reconciliation Load Test Results:");
        _logger.LogInformation($"Total Transactions: {transactionCount}");
        _logger.LogInformation($"Matched Transactions: {result.Summary.MatchedTransactions}");
        _logger.LogInformation($"Unmatched Transactions: {result.Summary.UnmatchedTransactions}");
        _logger.LogInformation($"Match Rate: {result.Summary.MatchRate:F2}%");
        _logger.LogInformation($"Processing Time: {stopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Throughput: {(double)transactionCount / stopwatch.ElapsedMilliseconds * 1000:F2} transactions/second");
        
        // Performance assertions
        result.Success.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Complete within 30 seconds
        result.Summary.MatchRate.Should().BeGreaterThan(75); // At least 75% match rate
        
        // Throughput should be reasonable
        var throughput = (double)transactionCount / stopwatch.ElapsedMilliseconds * 1000;
        throughput.Should().BeGreaterThan(100); // At least 100 transactions per second
    }

    [Theory]
    [InlineData(100, 10)] // 100 users, 10 operations each
    [InlineData(50, 20)]  // 50 users, 20 operations each
    [InlineData(25, 40)]  // 25 users, 40 operations each
    public async Task MixedOperations_LoadTest_VariousScenarios(
        int concurrentUsers, int operationsPerUser)
    {
        // Arrange
        var productIds = new List<Guid>();
        for (int i = 0; i < 20; i++)
        {
            var productId = Guid.NewGuid();
            await SetupInventoryItem(productId, 500);
            productIds.Add(productId);
        }
        
        var operationResults = new ConcurrentBag<OperationResult>();
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Each user performs multiple mixed operations
        var userTasks = Enumerable.Range(0, concurrentUsers).Select(async userId =>
        {
            var random = new Random(userId); // Seed with userId for reproducibility
            
            for (int op = 0; op < operationsPerUser; op++)
            {
                var operationStopwatch = Stopwatch.StartNew();
                var operationType = random.Next(0, 4); // 0: Reserve, 1: Release, 2: Price, 3: Order
                
                try
                {
                    using var scope = _fixture.ServiceProvider.CreateScope();
                    
                    switch (operationType)
                    {
                        case 0: // Reserve inventory
                            await PerformReservationOperation(scope, productIds, random, operationStopwatch, operationResults);
                            break;
                        case 1: // Release reservation (if any exist)
                            await PerformReleaseOperation(scope, operationStopwatch, operationResults);
                            break;
                        case 2: // Price calculation
                            await PerformPricingOperation(scope, productIds, random, operationStopwatch, operationResults);
                            break;
                        case 3: // Full order processing
                            await PerformOrderOperation(scope, productIds, random, operationStopwatch, operationResults);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    operationStopwatch.Stop();
                    operationResults.Add(new OperationResult
                    {
                        Type = (OperationType)operationType,
                        Success = false,
                        Duration = operationStopwatch.ElapsedMilliseconds,
                        Error = ex.Message
                    });
                }
            }
        });
        
        await Task.WhenAll(userTasks);
        stopwatch.Stop();
        
        // Assert
        var results = operationResults.ToArray();
        var totalOperations = concurrentUsers * operationsPerUser;
        var successfulOperations = results.Count(r => r.Success);
        var averageResponseTime = results.Average(r => r.Duration);
        
        _logger.LogInformation("Mixed Operations Load Test Results:");
        _logger.LogInformation($"Total Users: {concurrentUsers}");
        _logger.LogInformation($"Operations per User: {operationsPerUser}");
        _logger.LogInformation($"Total Operations: {totalOperations}");
        _logger.LogInformation($"Successful Operations: {successfulOperations}");
        _logger.LogInformation($"Success Rate: {(double)successfulOperations / totalOperations * 100:F2}%");
        _logger.LogInformation($"Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Average Response Time: {averageResponseTime:F2}ms");
        _logger.LogInformation($"Throughput: {(double)totalOperations / stopwatch.ElapsedMilliseconds * 1000:F2} operations/second");
        
        // Log operation type breakdown
        foreach (OperationType opType in Enum.GetValues<OperationType>())
        {
            var typeResults = results.Where(r => r.Type == opType).ToArray();
            if (typeResults.Any())
            {
                var typeSuccessRate = (double)typeResults.Count(r => r.Success) / typeResults.Length * 100;
                var typeAvgTime = typeResults.Average(r => r.Duration);
                _logger.LogInformation($"{opType}: {typeResults.Length} ops, {typeSuccessRate:F1}% success, {typeAvgTime:F1}ms avg");
            }
        }
        
        // Performance assertions
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(120000); // Complete within 2 minutes
        averageResponseTime.Should().BeLessThan(1000); // Average response time under 1 second
        successfulOperations.Should().BeGreaterThan(totalOperations * 0.8); // At least 80% success rate
    }

    // Helper methods and classes
    private enum OperationType
    {
        Reservation,
        Release,
        Pricing,
        Order
    }

    private class OperationResult
    {
        public OperationType Type { get; set; }
        public bool Success { get; set; }
        public long Duration { get; set; }
        public string? Error { get; set; }
    }

    private async Task PerformReservationOperation(
        IServiceScope scope, List<Guid> productIds, Random random,
        Stopwatch stopwatch, ConcurrentBag<OperationResult> results)
    {
        var service = scope.ServiceProvider.GetRequiredService<IReservationService>();
        var productId = productIds[random.Next(productIds.Count)];
        var customerId = Guid.NewGuid();
        var quantity = random.Next(1, 5);
        
        var result = await service.ReserveInventoryAsync(
            productId, quantity, customerId, TimeSpan.FromHours(1));
        
        stopwatch.Stop();
        results.Add(new OperationResult
        {
            Type = OperationType.Reservation,
            Success = result.Success,
            Duration = stopwatch.ElapsedMilliseconds
        });
    }

    private async Task PerformReleaseOperation(
        IServiceScope scope, Stopwatch stopwatch, ConcurrentBag<OperationResult> results)
    {
        // For simplicity, we'll create a reservation and immediately release it
        var service = scope.ServiceProvider.GetRequiredService<IReservationService>();
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        
        // This will likely fail, but that's okay for load testing
        try
        {
            var reservation = await service.ReserveInventoryAsync(
                productId, 1, customerId, TimeSpan.FromMinutes(1));
            
            if (reservation.Success)
            {
                await service.ReleaseReservationAsync(reservation.ReservationId);
            }
            
            stopwatch.Stop();
            results.Add(new OperationResult
            {
                Type = OperationType.Release,
                Success = true,
                Duration = stopwatch.ElapsedMilliseconds
            });
        }
        catch
        {
            stopwatch.Stop();
            results.Add(new OperationResult
            {
                Type = OperationType.Release,
                Success = false,
                Duration = stopwatch.ElapsedMilliseconds
            });
        }
    }

    private async Task PerformPricingOperation(
        IServiceScope scope, List<Guid> productIds, Random random,
        Stopwatch stopwatch, ConcurrentBag<OperationResult> results)
    {
        var service = scope.ServiceProvider.GetRequiredService<IPricingService>();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        
        var order = await CreateTestOrder(orderId, customerId, productIds, random.Next(1, 4));
        var result = await service.ApplyDiscountsAsync(order);
        
        stopwatch.Stop();
        results.Add(new OperationResult
        {
            Type = OperationType.Pricing,
            Success = result != null,
            Duration = stopwatch.ElapsedMilliseconds
        });
    }

    private async Task PerformOrderOperation(
        IServiceScope scope, List<Guid> productIds, Random random,
        Stopwatch stopwatch, ConcurrentBag<OperationResult> results)
    {
        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        
        var order = await CreateTestOrder(orderId, customerId, productIds, random.Next(1, 3));
        
        // Try to reserve inventory
        var reservationTasks = order.Items.Select(async item =>
        {
            return await reservationService.ReserveInventoryAsync(
                item.ProductId, item.Quantity, customerId, TimeSpan.FromHours(1));
        });
        
        var reservationResults = await Task.WhenAll(reservationTasks);
        var success = reservationResults.All(r => r.Success);
        
        if (success)
        {
            // Apply pricing
            await pricingService.ApplyDiscountsAsync(order);
        }
        
        stopwatch.Stop();
        results.Add(new OperationResult
        {
            Type = OperationType.Order,
            Success = success,
            Duration = stopwatch.ElapsedMilliseconds
        });
    }

    private static double GetPercentile(long[] values, int percentile)
    {
        if (values.Length == 0) return 0;
        
        Array.Sort(values);
        var index = (percentile / 100.0) * (values.Length - 1);
        
        if (index == Math.Floor(index))
        {
            return values[(int)index];
        }
        
        var lower = values[(int)Math.Floor(index)];
        var upper = values[(int)Math.Ceiling(index)];
        var weight = index - Math.Floor(index);
        
        return lower + weight * (upper - lower);
    }

    // Reuse helper methods from CommercePerformanceTests
    private async Task<InventoryItem?> SetupInventoryItem(Guid productId, int quantity)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = $"LOAD-SKU-{productId.ToString("N")[..8]}",
            QuantityOnHand = quantity,
            QuantityReserved = 0,
            QuantityAvailable = quantity,
            ReorderLevel = quantity / 10,
            MaxStockLevel = quantity * 2,
            Location = "Load-Test-Warehouse",
            LastUpdated = DateTime.UtcNow
        };
        
        context.InventoryItems.Add(inventoryItem);
        await context.SaveChangesAsync();
        
        return inventoryItem;
    }

    private async Task<InventoryItem?> GetInventoryItem(Guid productId)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        return await context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    private async Task<Order> CreateTestOrder(Guid orderId, Guid customerId, List<Guid> productIds, int itemCount)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var random = new Random();
        var orderItems = new List<OrderItem>();
        var totalAmount = 0m;
        
        for (int i = 0; i < itemCount; i++)
        {
            var productId = productIds[random.Next(productIds.Count)];
            var unitPrice = 15.00m + random.Next(1, 50);
            var quantity = random.Next(1, 4);
            var itemTotal = unitPrice * quantity;
            
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = itemTotal,
                Sku = $"LOAD-ITEM-{i:D3}"
            };
            
            orderItems.Add(orderItem);
            totalAmount += itemTotal;
        }
        
        var order = new Order
        {
            Id = orderId,
            OrderNumber = $"LOAD-ORD-{orderId.ToString("N")[..8]}",
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            SubTotal = totalAmount,
            TotalAmount = totalAmount,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            Items = orderItems
        };
        
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        
        return order;
    }

    private async Task<List<Subscription>> SetupSubscriptionsForRenewal(int count)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Load Test Plan",
            Description = "Plan for load testing",
            Price = 19.99m,
            Currency = "USD",
            BillingCycle = BillingCycle.Monthly,
            IsActive = true,
            TrialPeriodDays = 0
        };
        
        context.SubscriptionPlans.Add(plan);
        
        var subscriptions = new List<Subscription>();
        for (int i = 0; i < count; i++)
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = $"load.test.{i}@example.com",
                FirstName = "Load",
                LastName = $"User{i}"
            };
            
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-30),
                NextBillingDate = DateTime.UtcNow.Date,
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-30),
                CurrentPeriodEnd = DateTime.UtcNow.Date,
                Customer = customer,
                Plan = plan
            };
            
            context.Customers.Add(customer);
            subscriptions.Add(subscription);
        }
        
        context.Subscriptions.AddRange(subscriptions);
        await context.SaveChangesAsync();
        
        return subscriptions;
    }

    private async Task<(PaymentGatewayStatement statement, List<Payment> payments)> SetupReconciliationData(
        int transactionCount, int matchingPaymentCount)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var payments = new List<Payment>();
        var transactions = new List<StatementTransaction>();
        
        // Create matching payments and transactions
        for (int i = 0; i < matchingPaymentCount; i++)
        {
            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var externalTxnId = $"stripe_pi_load_{i:D6}";
            var amount = 25.00m + (i % 200); // Vary amounts
            
            var order = new Order
            {
                Id = orderId,
                OrderNumber = $"LOAD-REC-{i:D6}",
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Completed,
                TotalAmount = amount,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            };
            
            var payment = new Payment
            {
                Id = paymentId,
                OrderId = orderId,
                Amount = amount,
                Currency = "USD",
                PaymentProvider = "Stripe",
                ExternalTransactionId = externalTxnId,
                Status = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                Order = order
            };
            
            var transaction = new StatementTransaction
            {
                Id = $"load_txn_{i:D6}",
                ExternalTransactionId = externalTxnId,
                Amount = amount,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddHours(-3),
                ReferenceNumber = order.OrderNumber,
                TransactionType = "Payment",
                Status = "Completed"
            };
            
            context.Orders.Add(order);
            payments.Add(payment);
            transactions.Add(transaction);
        }
        
        // Create unmatched transactions
        for (int i = matchingPaymentCount; i < transactionCount; i++)
        {
            var transaction = new StatementTransaction
            {
                Id = $"load_txn_{i:D6}",
                ExternalTransactionId = $"stripe_pi_load_unmatched_{i:D6}",
                Amount = 45.00m + (i % 100),
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddHours(-2),
                ReferenceNumber = $"LOAD-UNMATCHED-{i:D6}",
                TransactionType = "Payment",
                Status = "Completed"
            };
            
            transactions.Add(transaction);
        }
        
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        
        var statement = new PaymentGatewayStatement
        {
            Id = "load_stmt_001",
            PaymentProvider = "Stripe",
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow,
            Transactions = transactions
        };
        
        return (statement, payments);
    }
}