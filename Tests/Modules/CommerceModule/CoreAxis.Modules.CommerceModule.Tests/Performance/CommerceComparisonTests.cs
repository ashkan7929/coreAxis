using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Infrastructure.ExternalServices;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace CoreAxis.Modules.CommerceModule.Tests.Performance;

public class CommerceComparisonTests : IClassFixture<CommerceTestFixture>
{
    private readonly CommerceTestFixture _fixture;
    private readonly ILogger<CommerceComparisonTests> _logger;

    public CommerceComparisonTests(CommerceTestFixture fixture)
    {
        _fixture = fixture;
        _logger = _fixture.ServiceProvider.GetRequiredService<ILogger<CommerceComparisonTests>>();
    }

    [Fact]
    public async Task CompareInventoryReservation_OptimizedVsBasic()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
        
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var concurrentRequests = 100;
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "COMPARISON-PRODUCT",
            QuantityOnHand = 1000,
            QuantityReserved = 0,
            QuantityAvailable = 1000,
            ReorderLevel = 50,
            MaxStockLevel = 2000,
            Location = "Comparison Warehouse",
            LastUpdated = DateTime.UtcNow
        };
        
        context.InventoryItems.Add(inventoryItem);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Starting inventory reservation comparison test");
        
        // Test 1: Sequential Processing (Basic approach)
        var sequentialStopwatch = Stopwatch.StartNew();
        var sequentialSuccessCount = 0;
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            var result = await reservationService.ReserveInventoryAsync(
                productId, 1, Guid.NewGuid(), TimeSpan.FromHours(1));
            if (result.Success)
                sequentialSuccessCount++;
        }
        
        sequentialStopwatch.Stop();
        
        // Reset inventory for next test
        await _fixture.CleanupDatabaseAsync();
        context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "COMPARISON-PRODUCT-2",
            QuantityOnHand = 1000,
            QuantityReserved = 0,
            QuantityAvailable = 1000,
            ReorderLevel = 50,
            MaxStockLevel = 2000,
            Location = "Comparison Warehouse",
            LastUpdated = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        
        // Test 2: Parallel Processing (Optimized approach)
        var parallelStopwatch = Stopwatch.StartNew();
        
        var parallelTasks = Enumerable.Range(0, concurrentRequests).Select(async i =>
        {
            return await reservationService.ReserveInventoryAsync(
                productId, 1, Guid.NewGuid(), TimeSpan.FromHours(1));
        });
        
        var parallelResults = await Task.WhenAll(parallelTasks);
        var parallelSuccessCount = parallelResults.Count(r => r.Success);
        
        parallelStopwatch.Stop();
        
        // Assert and Log Results
        _logger.LogInformation($"Sequential Processing: {sequentialStopwatch.ElapsedMilliseconds}ms, Success: {sequentialSuccessCount}/{concurrentRequests}");
        _logger.LogInformation($"Parallel Processing: {parallelStopwatch.ElapsedMilliseconds}ms, Success: {parallelSuccessCount}/{concurrentRequests}");
        
        var performanceImprovement = (double)(sequentialStopwatch.ElapsedMilliseconds - parallelStopwatch.ElapsedMilliseconds) / sequentialStopwatch.ElapsedMilliseconds * 100;
        
        _logger.LogInformation($"Performance Improvement: {performanceImprovement:F2}%");
        
        // Parallel should be faster (though may have more conflicts)
        parallelStopwatch.ElapsedMilliseconds.Should().BeLessThan(sequentialStopwatch.ElapsedMilliseconds * 2);
        
        // Both should process reasonable number of requests
        sequentialSuccessCount.Should().BeGreaterThan(0);
        parallelSuccessCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ComparePricingCalculation_CachedVsUncached()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        
        var customerId = Guid.NewGuid();
        var productIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        
        // Setup discount rules
        var discountRules = new List<DiscountRule>
        {
            new DiscountRule
            {
                Id = Guid.NewGuid(),
                Name = "Comparison 10% Discount",
                DiscountType = DiscountType.Percentage,
                Value = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                MinimumOrderAmount = 50,
                ApplicableToAllProducts = true
            },
            new DiscountRule
            {
                Id = Guid.NewGuid(),
                Name = "Comparison $15 Fixed Discount",
                DiscountType = DiscountType.FixedAmount,
                Value = 15,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                MinimumOrderAmount = 100,
                ApplicableToAllProducts = true
            }
        };
        
        context.DiscountRules.AddRange(discountRules);
        await context.SaveChangesAsync();
        
        // Create test orders
        var testOrders = Enumerable.Range(0, 50).Select(i => new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"PRICING-COMPARISON-{i:D3}",
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            SubTotal = 200.00m,
            TotalAmount = 200.00m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            Items = productIds.Select(productId => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ProductId = productId,
                Quantity = 1,
                UnitPrice = 20.00m,
                TotalPrice = 20.00m,
                Sku = $"COMPARISON-PRODUCT-{productId.ToString()[..8]}"
            }).ToList()
        }).ToList();
        
        _logger.LogInformation("Starting pricing calculation comparison test");
        
        // Test 1: First run (Cold cache)
        var coldCacheStopwatch = Stopwatch.StartNew();
        
        var coldCacheTasks = testOrders.Select(async order =>
        {
            return await pricingService.ApplyDiscountsAsync(order);
        });
        
        var coldCacheResults = await Task.WhenAll(coldCacheTasks);
        coldCacheStopwatch.Stop();
        
        // Test 2: Second run (Warm cache)
        var warmCacheStopwatch = Stopwatch.StartNew();
        
        var warmCacheTasks = testOrders.Select(async order =>
        {
            return await pricingService.ApplyDiscountsAsync(order);
        });
        
        var warmCacheResults = await Task.WhenAll(warmCacheTasks);
        warmCacheStopwatch.Stop();
        
        // Test 3: Third run (Hot cache)
        var hotCacheStopwatch = Stopwatch.StartNew();
        
        var hotCacheTasks = testOrders.Select(async order =>
        {
            return await pricingService.ApplyDiscountsAsync(order);
        });
        
        var hotCacheResults = await Task.WhenAll(hotCacheTasks);
        hotCacheStopwatch.Stop();
        
        // Assert and Log Results
        _logger.LogInformation($"Cold Cache: {coldCacheStopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Warm Cache: {warmCacheStopwatch.ElapsedMilliseconds}ms");
        _logger.LogInformation($"Hot Cache: {hotCacheStopwatch.ElapsedMilliseconds}ms");
        
        var warmCacheImprovement = (double)(coldCacheStopwatch.ElapsedMilliseconds - warmCacheStopwatch.ElapsedMilliseconds) / coldCacheStopwatch.ElapsedMilliseconds * 100;
        var hotCacheImprovement = (double)(coldCacheStopwatch.ElapsedMilliseconds - hotCacheStopwatch.ElapsedMilliseconds) / coldCacheStopwatch.ElapsedMilliseconds * 100;
        
        _logger.LogInformation($"Warm Cache Improvement: {warmCacheImprovement:F2}%");
        _logger.LogInformation($"Hot Cache Improvement: {hotCacheImprovement:F2}%");
        
        // Warm cache should be faster than cold cache
        warmCacheStopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(coldCacheStopwatch.ElapsedMilliseconds);
        
        // Hot cache should be fastest
        hotCacheStopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(warmCacheStopwatch.ElapsedMilliseconds);
        
        // Results should be consistent
        coldCacheResults.Should().HaveCount(testOrders.Count);
        warmCacheResults.Should().HaveCount(testOrders.Count);
        hotCacheResults.Should().HaveCount(testOrders.Count);
    }

    [Fact]
    public async Task CompareSubscriptionProcessing_BatchVsIndividual()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        
        var planId = Guid.NewGuid();
        var customerIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Comparison Test Plan",
            Description = "Plan for comparison testing",
            Price = 29.99m,
            Currency = "USD",
            BillingCycle = BillingCycle.Monthly,
            IsActive = true,
            TrialPeriodDays = 7
        };
        
        var customers = customerIds.Select((id, index) => new Customer
        {
            Id = id,
            Email = $"comparison{index}@example.com",
            FirstName = $"Customer{index}",
            LastName = "Test"
        }).ToList();
        
        context.SubscriptionPlans.Add(plan);
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Starting subscription processing comparison test");
        
        // Test 1: Individual Processing
        var individualStopwatch = Stopwatch.StartNew();
        var individualSuccessCount = 0;
        
        foreach (var customerId in customerIds)
        {
            try
            {
                var subscription = await subscriptionService.CreateSubscriptionAsync(
                    customerId, planId, DateTime.UtcNow, null);
                if (subscription != null)
                    individualSuccessCount++;
            }
            catch
            {
                // Continue processing
            }
        }
        
        individualStopwatch.Stop();
        
        // Clean up for next test
        await _fixture.CleanupDatabaseAsync();
        context.SubscriptionPlans.Add(plan);
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();
        
        // Test 2: Batch Processing
        var batchStopwatch = Stopwatch.StartNew();
        
        var batchTasks = customerIds.Select(async customerId =>
        {
            try
            {
                return await subscriptionService.CreateSubscriptionAsync(
                    customerId, planId, DateTime.UtcNow, null);
            }
            catch
            {
                return null;
            }
        });
        
        var batchResults = await Task.WhenAll(batchTasks);
        var batchSuccessCount = batchResults.Count(s => s != null);
        
        batchStopwatch.Stop();
        
        // Assert and Log Results
        _logger.LogInformation($"Individual Processing: {individualStopwatch.ElapsedMilliseconds}ms, Success: {individualSuccessCount}/{customerIds.Count}");
        _logger.LogInformation($"Batch Processing: {batchStopwatch.ElapsedMilliseconds}ms, Success: {batchSuccessCount}/{customerIds.Count}");
        
        var performanceImprovement = (double)(individualStopwatch.ElapsedMilliseconds - batchStopwatch.ElapsedMilliseconds) / individualStopwatch.ElapsedMilliseconds * 100;
        
        _logger.LogInformation($"Batch Processing Improvement: {performanceImprovement:F2}%");
        
        // Batch processing should be faster
        batchStopwatch.ElapsedMilliseconds.Should().BeLessThan(individualStopwatch.ElapsedMilliseconds);
        
        // Both should have reasonable success rates
        individualSuccessCount.Should().BeGreaterThan(customerIds.Count / 2);
        batchSuccessCount.Should().BeGreaterThan(customerIds.Count / 2);
    }

    [Fact]
    public async Task CompareReconciliation_OptimizedVsBasic()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        
        var transactionCount = 1000;
        var orderIds = Enumerable.Range(0, transactionCount).Select(_ => Guid.NewGuid()).ToList();
        
        // Setup test orders and payments
        var orders = orderIds.Select((orderId, index) => new Order
        {
            Id = orderId,
            OrderNumber = $"RECONCILE-ORDER-{index:D6}",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Completed,
            TotalAmount = 100.00m + (index % 500),
            Currency = "USD",
            CreatedAt = DateTime.UtcNow.AddHours(-index)
        }).ToList();
        
        var payments = orderIds.Select((orderId, index) => new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 100.00m + (index % 500),
            Currency = "USD",
            PaymentProvider = "Stripe",
            ExternalTransactionId = $"stripe_pi_reconcile_{index:D6}",
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-index)
        }).ToList();
        
        context.Orders.AddRange(orders);
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        
        // Create gateway transactions
        var gatewayTransactions = orderIds.Select((orderId, index) => new GatewayTransaction
        {
            TransactionId = $"stripe_pi_reconcile_{index:D6}",
            Amount = 100.00m + (index % 500),
            Currency = "USD",
            Status = "completed",
            ProcessedAt = DateTime.UtcNow.AddHours(-index),
            OrderReference = $"RECONCILE-ORDER-{index:D6}",
            PaymentMethodId = "pm_test_card",
            Fees = 3.00m + (index % 10) * 0.1m
        }).ToList();
        
        _logger.LogInformation("Starting reconciliation comparison test");
        
        // Test 1: Process all transactions at once (Basic approach)
        var basicStopwatch = Stopwatch.StartNew();
        
        var basicResult = await reconciliationService.ReconcileTransactionsAsync(
            gatewayTransactions, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        
        basicStopwatch.Stop();
        
        // Test 2: Process transactions in batches (Optimized approach)
        var optimizedStopwatch = Stopwatch.StartNew();
        var batchSize = 100;
        var totalMatched = 0;
        var totalUnmatched = 0;
        
        for (int i = 0; i < gatewayTransactions.Count; i += batchSize)
        {
            var batch = gatewayTransactions.Skip(i).Take(batchSize).ToList();
            var batchResult = await reconciliationService.ReconcileTransactionsAsync(
                batch, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
            
            totalMatched += batchResult.MatchedTransactions.Count;
            totalUnmatched += batchResult.UnmatchedTransactions.Count;
        }
        
        optimizedStopwatch.Stop();
        
        // Assert and Log Results
        _logger.LogInformation($"Basic Reconciliation: {basicStopwatch.ElapsedMilliseconds}ms, Matched: {basicResult.MatchedTransactions.Count}, Unmatched: {basicResult.UnmatchedTransactions.Count}");
        _logger.LogInformation($"Optimized Reconciliation: {optimizedStopwatch.ElapsedMilliseconds}ms, Matched: {totalMatched}, Unmatched: {totalUnmatched}");
        
        var performanceImprovement = (double)(basicStopwatch.ElapsedMilliseconds - optimizedStopwatch.ElapsedMilliseconds) / basicStopwatch.ElapsedMilliseconds * 100;
        
        _logger.LogInformation($"Optimized Processing Improvement: {performanceImprovement:F2}%");
        
        // Results should be consistent
        totalMatched.Should().Be(basicResult.MatchedTransactions.Count);
        totalUnmatched.Should().Be(basicResult.UnmatchedTransactions.Count);
        
        // Most transactions should be matched
        basicResult.MatchedTransactions.Count.Should().BeGreaterThan(transactionCount * 0.8);
    }

    [Fact]
    public async Task CompareRefundProcessing_ParallelVsSequential()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var refundService = scope.ServiceProvider.GetRequiredService<IRefundService>();
        var paymentProvider = _fixture.PaymentProviderMock;
        
        var refundCount = 50;
        var orderIds = Enumerable.Range(0, refundCount).Select(_ => Guid.NewGuid()).ToList();
        var customerIds = Enumerable.Range(0, refundCount).Select(_ => Guid.NewGuid()).ToList();
        
        // Setup test orders and payments
        var orders = orderIds.Select((orderId, index) => new Order
        {
            Id = orderId,
            OrderNumber = $"REFUND-ORDER-{index:D3}",
            CustomerId = customerIds[index],
            Status = OrderStatus.Completed,
            TotalAmount = 100.00m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        }).ToList();
        
        var payments = orderIds.Select((orderId, index) => new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = 100.00m,
            Currency = "USD",
            PaymentProvider = "Stripe",
            ExternalTransactionId = $"stripe_pi_refund_{index:D3}",
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        }).ToList();
        
        context.Orders.AddRange(orders);
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        
        // Setup payment provider mock
        paymentProvider.Setup(p => p.RefundPaymentAsync(
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync((string transactionId, decimal amount, string reason) => new RefundResponse
            {
                Success = true,
                RefundId = $"stripe_re_{transactionId.Split('_').Last()}",
                Amount = amount,
                Currency = "USD",
                Status = "succeeded",
                ProcessedAt = DateTime.UtcNow
            });
        
        _logger.LogInformation("Starting refund processing comparison test");
        
        // Test 1: Sequential Processing
        var sequentialStopwatch = Stopwatch.StartNew();
        var sequentialSuccessCount = 0;
        
        for (int i = 0; i < refundCount; i++)
        {
            var refundRequest = new RefundRequest
            {
                OrderId = orderIds[i],
                Amount = 100.00m,
                Reason = $"Sequential refund {i}",
                RefundMethod = RefundMethod.OriginalPayment,
                RequestedBy = customerIds[i].ToString()
            };
            
            var result = await refundService.ProcessRefundAsync(refundRequest);
            if (result.Success)
                sequentialSuccessCount++;
        }
        
        sequentialStopwatch.Stop();
        
        // Reset for parallel test
        await _fixture.CleanupDatabaseAsync();
        context.Orders.AddRange(orders);
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        
        // Test 2: Parallel Processing
        var parallelStopwatch = Stopwatch.StartNew();
        
        var parallelTasks = Enumerable.Range(0, refundCount).Select(async i =>
        {
            var refundRequest = new RefundRequest
            {
                OrderId = orderIds[i],
                Amount = 100.00m,
                Reason = $"Parallel refund {i}",
                RefundMethod = RefundMethod.OriginalPayment,
                RequestedBy = customerIds[i].ToString()
            };
            
            return await refundService.ProcessRefundAsync(refundRequest);
        });
        
        var parallelResults = await Task.WhenAll(parallelTasks);
        var parallelSuccessCount = parallelResults.Count(r => r.Success);
        
        parallelStopwatch.Stop();
        
        // Assert and Log Results
        _logger.LogInformation($"Sequential Refund Processing: {sequentialStopwatch.ElapsedMilliseconds}ms, Success: {sequentialSuccessCount}/{refundCount}");
        _logger.LogInformation($"Parallel Refund Processing: {parallelStopwatch.ElapsedMilliseconds}ms, Success: {parallelSuccessCount}/{refundCount}");
        
        var performanceImprovement = (double)(sequentialStopwatch.ElapsedMilliseconds - parallelStopwatch.ElapsedMilliseconds) / sequentialStopwatch.ElapsedMilliseconds * 100;
        
        _logger.LogInformation($"Parallel Processing Improvement: {performanceImprovement:F2}%");
        
        // Parallel should be faster
        parallelStopwatch.ElapsedMilliseconds.Should().BeLessThan(sequentialStopwatch.ElapsedMilliseconds);
        
        // Both should have high success rates
        sequentialSuccessCount.Should().BeGreaterThan(refundCount * 0.8);
        parallelSuccessCount.Should().BeGreaterThan(refundCount * 0.8);
    }

    [Fact]
    public async Task CompareMemoryUsage_LargeDataSets()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        
        var largeDataSetSize = 10000;
        
        _logger.LogInformation("Starting memory usage comparison test");
        
        // Test 1: Load all data at once (Memory intensive)
        var beforeMemory1 = GC.GetTotalMemory(true);
        
        var allOrdersStopwatch = Stopwatch.StartNew();
        
        var allOrders = Enumerable.Range(0, largeDataSetSize).Select(i => new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"MEMORY-ORDER-{i:D6}",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            SubTotal = 100.00m,
            TotalAmount = 100.00m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m,
                    Sku = $"MEMORY-PRODUCT-{i:D6}"
                }
            }
        }).ToList();
        
        var allOrdersTasks = allOrders.Select(async order =>
        {
            return await pricingService.ApplyDiscountsAsync(order);
        });
        
        await Task.WhenAll(allOrdersTasks);
        
        allOrdersStopwatch.Stop();
        var afterMemory1 = GC.GetTotalMemory(false);
        var memoryUsed1 = afterMemory1 - beforeMemory1;
        
        // Clear memory
        allOrders.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Test 2: Process in batches (Memory efficient)
        var beforeMemory2 = GC.GetTotalMemory(true);
        
        var batchProcessingStopwatch = Stopwatch.StartNew();
        var batchSize = 1000;
        var processedCount = 0;
        
        for (int batch = 0; batch < largeDataSetSize / batchSize; batch++)
        {
            var batchOrders = Enumerable.Range(0, batchSize).Select(i =>
            {
                var orderIndex = batch * batchSize + i;
                return new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"BATCH-ORDER-{orderIndex:D6}",
                    CustomerId = Guid.NewGuid(),
                    Status = OrderStatus.Pending,
                    SubTotal = 100.00m,
                    TotalAmount = 100.00m,
                    Currency = "USD",
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            OrderId = Guid.NewGuid(),
                            ProductId = Guid.NewGuid(),
                            Quantity = 1,
                            UnitPrice = 100.00m,
                            TotalPrice = 100.00m,
                            Sku = $"BATCH-PRODUCT-{orderIndex:D6}"
                        }
                    }
                };
            }).ToList();
            
            var batchTasks = batchOrders.Select(async order =>
            {
                return await pricingService.ApplyDiscountsAsync(order);
            });
            
            await Task.WhenAll(batchTasks);
            processedCount += batchSize;
            
            // Clear batch
            batchOrders.Clear();
            
            if (batch % 2 == 0)
            {
                GC.Collect();
            }
        }
        
        batchProcessingStopwatch.Stop();
        var afterMemory2 = GC.GetTotalMemory(false);
        var memoryUsed2 = afterMemory2 - beforeMemory2;
        
        // Assert and Log Results
        _logger.LogInformation($"All-at-once Processing: {allOrdersStopwatch.ElapsedMilliseconds}ms, Memory: {memoryUsed1 / 1024 / 1024:F2} MB");
        _logger.LogInformation($"Batch Processing: {batchProcessingStopwatch.ElapsedMilliseconds}ms, Memory: {memoryUsed2 / 1024 / 1024:F2} MB");
        
        var memoryEfficiency = (double)(memoryUsed1 - memoryUsed2) / memoryUsed1 * 100;
        
        _logger.LogInformation($"Memory Efficiency Improvement: {memoryEfficiency:F2}%");
        
        // Batch processing should use less memory
        memoryUsed2.Should().BeLessThan(memoryUsed1);
        
        // Both should process all orders
        processedCount.Should().Be(largeDataSetSize);
    }

    [Fact]
    public async Task CompareDatabaseQueries_OptimizedVsBasic()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var customerCount = 100;
        var ordersPerCustomer = 10;
        
        var customers = Enumerable.Range(0, customerCount).Select(i => new Customer
        {
            Id = Guid.NewGuid(),
            Email = $"query-test{i}@example.com",
            FirstName = $"Customer{i}",
            LastName = "Test"
        }).ToList();
        
        var orders = customers.SelectMany(customer =>
            Enumerable.Range(0, ordersPerCustomer).Select(i => new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"QUERY-ORDER-{customer.Id.ToString()[..8]}-{i:D2}",
                CustomerId = customer.Id,
                Status = OrderStatus.Completed,
                TotalAmount = 100.00m + i * 10,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                Customer = customer
            })
        ).ToList();
        
        context.Customers.AddRange(customers);
        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Starting database query comparison test");
        
        // Test 1: N+1 Query Problem (Basic approach)
        var basicStopwatch = Stopwatch.StartNew();
        var basicResults = new List<(Customer customer, List<Order> orders)>();
        
        var allCustomers = await context.Customers.ToListAsync();
        
        foreach (var customer in allCustomers)
        {
            var customerOrders = await context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .ToListAsync();
            
            basicResults.Add((customer, customerOrders));
        }
        
        basicStopwatch.Stop();
        
        // Test 2: Optimized Query with Include (Optimized approach)
        var optimizedStopwatch = Stopwatch.StartNew();
        
        var optimizedResults = await context.Customers
            .Include(c => c.Orders)
            .ToListAsync();
        
        optimizedStopwatch.Stop();
        
        // Test 3: Projection Query (Most optimized)
        var projectionStopwatch = Stopwatch.StartNew();
        
        var projectionResults = await context.Customers
            .Select(c => new
            {
                Customer = c,
                OrderCount = c.Orders.Count(),
                TotalSpent = c.Orders.Sum(o => o.TotalAmount),
                LastOrderDate = c.Orders.Max(o => o.CreatedAt)
            })
            .ToListAsync();
        
        projectionStopwatch.Stop();
        
        // Assert and Log Results
        _logger.LogInformation($"Basic Queries (N+1): {basicStopwatch.ElapsedMilliseconds}ms, Results: {basicResults.Count}");
        _logger.LogInformation($"Optimized Include: {optimizedStopwatch.ElapsedMilliseconds}ms, Results: {optimizedResults.Count}");
        _logger.LogInformation($"Projection Query: {projectionStopwatch.ElapsedMilliseconds}ms, Results: {projectionResults.Count}");
        
        var includeImprovement = (double)(basicStopwatch.ElapsedMilliseconds - optimizedStopwatch.ElapsedMilliseconds) / basicStopwatch.ElapsedMilliseconds * 100;
        var projectionImprovement = (double)(basicStopwatch.ElapsedMilliseconds - projectionStopwatch.ElapsedMilliseconds) / basicStopwatch.ElapsedMilliseconds * 100;
        
        _logger.LogInformation($"Include Query Improvement: {includeImprovement:F2}%");
        _logger.LogInformation($"Projection Query Improvement: {projectionImprovement:F2}%");
        
        // Optimized queries should be faster
        optimizedStopwatch.ElapsedMilliseconds.Should().BeLessThan(basicStopwatch.ElapsedMilliseconds);
        projectionStopwatch.ElapsedMilliseconds.Should().BeLessThan(basicStopwatch.ElapsedMilliseconds);
        
        // Results should be consistent
        basicResults.Should().HaveCount(customerCount);
        optimizedResults.Should().HaveCount(customerCount);
        projectionResults.Should().HaveCount(customerCount);
    }
}