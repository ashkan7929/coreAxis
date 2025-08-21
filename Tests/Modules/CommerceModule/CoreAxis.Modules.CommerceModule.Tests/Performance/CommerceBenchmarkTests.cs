using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Infrastructure.ExternalServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace CoreAxis.Modules.CommerceModule.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class CommerceBenchmarkTests
{
    private CommerceTestFixture _fixture = null!;
    private IServiceScope _scope = null!;
    private IReservationService _reservationService = null!;
    private IPricingService _pricingService = null!;
    private ISubscriptionService _subscriptionService = null!;
    private IReconciliationService _reconciliationService = null!;
    private IRefundService _refundService = null!;
    private ICommerceDbContext _context = null!;
    
    private List<Guid> _productIds = null!;
    private List<Guid> _customerIds = null!;
    private List<Guid> _orderIds = null!;
    private List<Guid> _subscriptionIds = null!;
    private List<Order> _testOrders = null!;
    private List<InventoryItem> _testInventoryItems = null!;
    private List<DiscountRule> _testDiscountRules = null!;
    private List<SubscriptionPlan> _testSubscriptionPlans = null!;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _fixture = new CommerceTestFixture();
        _scope = _fixture.ServiceProvider.CreateScope();
        
        _reservationService = _scope.ServiceProvider.GetRequiredService<IReservationService>();
        _pricingService = _scope.ServiceProvider.GetRequiredService<IPricingService>();
        _subscriptionService = _scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        _reconciliationService = _scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        _refundService = _scope.ServiceProvider.GetRequiredService<IRefundService>();
        _context = _scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        await SetupTestData();
    }
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        _scope?.Dispose();
        await _fixture.DisposeAsync();
    }
    
    private async Task SetupTestData()
    {
        // Generate test IDs
        _productIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        _customerIds = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        _orderIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        _subscriptionIds = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()).ToList();
        
        // Setup inventory items
        _testInventoryItems = _productIds.Select((productId, index) => new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = $"BENCHMARK-PRODUCT-{index:D4}",
            QuantityOnHand = 1000,
            QuantityReserved = 0,
            QuantityAvailable = 1000,
            ReorderLevel = 50,
            MaxStockLevel = 2000,
            Location = $"Warehouse-{index % 10}",
            LastUpdated = DateTime.UtcNow
        }).ToList();
        
        // Setup discount rules
        _testDiscountRules = new List<DiscountRule>
        {
            new DiscountRule
            {
                Id = Guid.NewGuid(),
                Name = "Benchmark 10% Discount",
                Description = "10% discount for benchmark testing",
                DiscountType = DiscountType.Percentage,
                Value = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(30),
                MinimumOrderAmount = 50,
                MaximumDiscountAmount = 100,
                UsageLimit = 10000,
                UsageCount = 0,
                ApplicableToAllProducts = true
            },
            new DiscountRule
            {
                Id = Guid.NewGuid(),
                Name = "Benchmark $20 Fixed Discount",
                Description = "$20 fixed discount for benchmark testing",
                DiscountType = DiscountType.FixedAmount,
                Value = 20,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(30),
                MinimumOrderAmount = 100,
                MaximumDiscountAmount = 20,
                UsageLimit = 10000,
                UsageCount = 0,
                ApplicableToAllProducts = true
            }
        };
        
        // Setup subscription plans
        _testSubscriptionPlans = new List<SubscriptionPlan>
        {
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Benchmark Basic Plan",
                Description = "Basic plan for benchmark testing",
                Price = 29.99m,
                Currency = "USD",
                BillingCycle = BillingCycle.Monthly,
                IsActive = true,
                TrialPeriodDays = 7
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Benchmark Premium Plan",
                Description = "Premium plan for benchmark testing",
                Price = 99.99m,
                Currency = "USD",
                BillingCycle = BillingCycle.Monthly,
                IsActive = true,
                TrialPeriodDays = 14
            }
        };
        
        // Setup test orders
        _testOrders = _orderIds.Select((orderId, index) => new Order
        {
            Id = orderId,
            OrderNumber = $"BENCHMARK-ORDER-{index:D6}",
            CustomerId = _customerIds[index % _customerIds.Count],
            Status = OrderStatus.Pending,
            SubTotal = 100.00m + (index % 500),
            TotalAmount = 100.00m + (index % 500),
            Currency = "USD",
            CreatedAt = DateTime.UtcNow.AddMinutes(-index),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = _productIds[index % _productIds.Count],
                    Quantity = 1 + (index % 5),
                    UnitPrice = 20.00m + (index % 100),
                    TotalPrice = (20.00m + (index % 100)) * (1 + (index % 5)),
                    Sku = $"BENCHMARK-PRODUCT-{index % _productIds.Count:D4}"
                }
            }
        }).ToList();
        
        // Add test data to context
        _context.InventoryItems.AddRange(_testInventoryItems);
        _context.DiscountRules.AddRange(_testDiscountRules);
        _context.SubscriptionPlans.AddRange(_testSubscriptionPlans);
        _context.Orders.AddRange(_testOrders);
        
        await _context.SaveChangesAsync();
    }
    
    [Benchmark]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(100)]
    public async Task<int> ReserveInventory_SingleProduct(int concurrentReservations)
    {
        var productId = _productIds[0];
        var tasks = new List<Task<ReservationResult>>();
        
        for (int i = 0; i < concurrentReservations; i++)
        {
            var customerId = _customerIds[i % _customerIds.Count];
            tasks.Add(_reservationService.ReserveInventoryAsync(
                productId, 1, customerId, TimeSpan.FromHours(1)));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r.Success);
    }
    
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task<int> ReserveInventory_MultipleProducts(int productCount)
    {
        var tasks = new List<Task<ReservationResult>>();
        
        for (int i = 0; i < productCount; i++)
        {
            var productId = _productIds[i];
            var customerId = _customerIds[i % _customerIds.Count];
            tasks.Add(_reservationService.ReserveInventoryAsync(
                productId, 1, customerId, TimeSpan.FromHours(1)));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r.Success);
    }
    
    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task<decimal> ApplyDiscounts_SimpleOrder(int itemCount)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"BENCHMARK-DISCOUNT-{DateTime.UtcNow.Ticks}",
            CustomerId = _customerIds[0],
            Status = OrderStatus.Pending,
            SubTotal = itemCount * 50.00m,
            TotalAmount = itemCount * 50.00m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            Items = Enumerable.Range(0, itemCount).Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ProductId = _productIds[i % _productIds.Count],
                Quantity = 1,
                UnitPrice = 50.00m,
                TotalPrice = 50.00m,
                Sku = $"BENCHMARK-PRODUCT-{i:D4}"
            }).ToList()
        };
        
        var result = await _pricingService.ApplyDiscountsAsync(order);
        return result.FinalAmount;
    }
    
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task<int> ApplyDiscounts_BatchOrders(int orderCount)
    {
        var tasks = new List<Task<PricingResult>>();
        
        for (int i = 0; i < orderCount; i++)
        {
            var order = _testOrders[i % _testOrders.Count];
            tasks.Add(_pricingService.ApplyDiscountsAsync(order));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Length;
    }
    
    [Benchmark]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(50)]
    public async Task<int> CreateSubscriptions(int subscriptionCount)
    {
        var tasks = new List<Task<Subscription>>();
        var planId = _testSubscriptionPlans[0].Id;
        
        for (int i = 0; i < subscriptionCount; i++)
        {
            var customerId = _customerIds[i % _customerIds.Count];
            tasks.Add(_subscriptionService.CreateSubscriptionAsync(
                customerId, planId, DateTime.UtcNow, null));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Count(s => s != null);
    }
    
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(500)]
    public async Task<int> ProcessReconciliation_BatchTransactions(int transactionCount)
    {
        var gatewayTransactions = Enumerable.Range(0, transactionCount).Select(i => new GatewayTransaction
        {
            TransactionId = $"gateway_txn_{i:D6}",
            Amount = 100.00m + (i % 500),
            Currency = "USD",
            Status = "completed",
            ProcessedAt = DateTime.UtcNow.AddMinutes(-i),
            OrderReference = $"BENCHMARK-ORDER-{i:D6}",
            PaymentMethodId = "pm_benchmark_card",
            Fees = 3.00m + (i % 10) * 0.1m
        }).ToList();
        
        var result = await _reconciliationService.ReconcileTransactionsAsync(
            gatewayTransactions, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        
        return result.MatchedTransactions.Count;
    }
    
    [Benchmark]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(50)]
    public async Task<int> ProcessRefunds(int refundCount)
    {
        var tasks = new List<Task<RefundResult>>();
        
        for (int i = 0; i < refundCount; i++)
        {
            var orderId = _orderIds[i % _orderIds.Count];
            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                Amount = 50.00m,
                Reason = $"Benchmark refund {i}",
                RefundMethod = RefundMethod.OriginalPayment,
                RequestedBy = _customerIds[i % _customerIds.Count].ToString()
            };
            
            tasks.Add(_refundService.ProcessRefundAsync(refundRequest));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r.Success);
    }
    
    [Benchmark]
    public async Task<int> InventoryLedger_HighVolumeTransactions()
    {
        var productId = _productIds[0];
        var tasks = new List<Task>();
        var transactionCount = 1000;
        
        // Simulate high-volume inventory transactions
        for (int i = 0; i < transactionCount; i++)
        {
            var customerId = _customerIds[i % _customerIds.Count];
            var quantity = (i % 2 == 0) ? 1 : -1; // Alternate between additions and subtractions
            
            tasks.Add(Task.Run(async () =>
            {
                if (quantity > 0)
                {
                    await _reservationService.ReserveInventoryAsync(
                        productId, 1, customerId, TimeSpan.FromMinutes(1));
                }
                else
                {
                    // Simulate inventory return/cancellation
                    var reservation = await _reservationService.ReserveInventoryAsync(
                        productId, 1, customerId, TimeSpan.FromMinutes(1));
                    if (reservation.Success)
                    {
                        await _reservationService.CancelReservationAsync(reservation.ReservationId);
                    }
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        return transactionCount;
    }
    
    [Benchmark]
    public async Task<int> ConcurrentOrderProcessing()
    {
        var concurrentOrders = 100;
        var tasks = new List<Task<bool>>();
        
        for (int i = 0; i < concurrentOrders; i++)
        {
            var order = _testOrders[i % _testOrders.Count];
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Simulate full order processing pipeline
                    
                    // 1. Reserve inventory
                    var reservationTasks = order.Items.Select(async item =>
                    {
                        return await _reservationService.ReserveInventoryAsync(
                            item.ProductId, item.Quantity, order.CustomerId, TimeSpan.FromHours(1));
                    });
                    
                    var reservationResults = await Task.WhenAll(reservationTasks);
                    
                    if (!reservationResults.All(r => r.Success))
                        return false;
                    
                    // 2. Apply pricing and discounts
                    var pricingResult = await _pricingService.ApplyDiscountsAsync(order);
                    
                    if (pricingResult == null)
                        return false;
                    
                    // 3. Confirm reservations
                    var confirmationTasks = reservationResults.Select(async r =>
                    {
                        return await _reservationService.ConfirmReservationAsync(r.ReservationId);
                    });
                    
                    var confirmationResults = await Task.WhenAll(confirmationTasks);
                    
                    return confirmationResults.All(c => c);
                }
                catch
                {
                    return false;
                }
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }
    
    [Benchmark]
    public async Task<int> SubscriptionRenewal_BatchProcessing()
    {
        var renewalCount = 200;
        var tasks = new List<Task<bool>>();
        
        // Create subscriptions for renewal
        var subscriptions = new List<Subscription>();
        for (int i = 0; i < renewalCount; i++)
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CustomerId = _customerIds[i % _customerIds.Count],
                PlanId = _testSubscriptionPlans[i % _testSubscriptionPlans.Count].Id,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-30),
                NextBillingDate = DateTime.UtcNow, // Due for renewal
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-30),
                CurrentPeriodEnd = DateTime.UtcNow,
                Plan = _testSubscriptionPlans[i % _testSubscriptionPlans.Count]
            };
            subscriptions.Add(subscription);
        }
        
        _context.Subscriptions.AddRange(subscriptions);
        await _context.SaveChangesAsync();
        
        // Process renewals
        for (int i = 0; i < renewalCount; i++)
        {
            var subscription = subscriptions[i];
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await _subscriptionService.RenewSubscriptionAsync(
                        subscription.Id, DateTime.UtcNow.AddDays(30));
                }
                catch
                {
                    return false;
                }
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }
    
    [Benchmark]
    public async Task<TimeSpan> DatabaseQuery_InventoryLookup()
    {
        var startTime = DateTime.UtcNow;
        var lookupCount = 1000;
        
        var tasks = Enumerable.Range(0, lookupCount).Select(async i =>
        {
            var productId = _productIds[i % _productIds.Count];
            return await _context.InventoryItems
                .FirstOrDefaultAsync(inv => inv.ProductId == productId);
        });
        
        await Task.WhenAll(tasks);
        
        return DateTime.UtcNow - startTime;
    }
    
    [Benchmark]
    public async Task<TimeSpan> DatabaseQuery_OrderHistory()
    {
        var startTime = DateTime.UtcNow;
        var queryCount = 100;
        
        var tasks = Enumerable.Range(0, queryCount).Select(async i =>
        {
            var customerId = _customerIds[i % _customerIds.Count];
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();
        });
        
        await Task.WhenAll(tasks);
        
        return DateTime.UtcNow - startTime;
    }
    
    [Benchmark]
    public async Task<int> MemoryEfficiency_LargeDataSet()
    {
        var largeOrderCount = 10000;
        var processedCount = 0;
        
        // Process orders in batches to test memory efficiency
        var batchSize = 100;
        
        for (int batch = 0; batch < largeOrderCount / batchSize; batch++)
        {
            var batchOrders = new List<Order>();
            
            for (int i = 0; i < batchSize; i++)
            {
                var orderIndex = batch * batchSize + i;
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"MEMORY-TEST-{orderIndex:D6}",
                    CustomerId = _customerIds[orderIndex % _customerIds.Count],
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
                            ProductId = _productIds[orderIndex % _productIds.Count],
                            Quantity = 1,
                            UnitPrice = 100.00m,
                            TotalPrice = 100.00m,
                            Sku = $"MEMORY-PRODUCT-{orderIndex % _productIds.Count:D4}"
                        }
                    }
                };
                
                batchOrders.Add(order);
            }
            
            // Process batch
            var pricingTasks = batchOrders.Select(async order =>
            {
                return await _pricingService.ApplyDiscountsAsync(order);
            });
            
            await Task.WhenAll(pricingTasks);
            processedCount += batchSize;
            
            // Clear batch to free memory
            batchOrders.Clear();
            
            // Force garbage collection to test memory efficiency
            if (batch % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        return processedCount;
    }
}

// Benchmark runner class
public static class BenchmarkRunner
{
    public static void RunBenchmarks()
    {
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<CommerceBenchmarkTests>();
        Console.WriteLine(summary);
    }
}