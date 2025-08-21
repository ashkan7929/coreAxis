using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.ValueObjects;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using CoreAxis.Modules.CommerceModule.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace CoreAxis.Modules.CommerceModule.Tests.Performance
{
    /// <summary>
    /// Performance tests for Commerce Module
    /// Tests system performance under various load conditions
    /// </summary>
    [Trait(TestTraits.Category, TestCategories.Performance)]
    public class CommercePerformanceTests : CommerceTestBase
    {
        private const int PERFORMANCE_THRESHOLD_MS = 1000;
        private const int BULK_OPERATION_SIZE = 100;
        private const int CONCURRENT_OPERATION_COUNT = 10;
        private const int STRESS_TEST_ITERATIONS = 1000;

        #region Order Processing Performance Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task OrderCreation_ShouldMeetPerformanceThreshold()
        {
            // Performance test for order creation
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItems = new List<InventoryItem>();
            
            for (int i = 0; i < 10; i++)
            {
                inventoryItems.Add(await CreateTestInventoryItemAsync($"Product {i}", quantity: 1000));
            }

            var orderItems = inventoryItems.Select(item => new OrderItem
            {
                InventoryItemId = item.Id,
                Quantity = 1,
                UnitPrice = new Money(25.00m, "USD")
            }).ToList();

            // Act & Assert - Measure order creation performance
            var stopwatch = Stopwatch.StartNew();
            
            var order = await OrderService.CreateOrderAsync(
                customer.Id,
                orderItems,
                customer.DefaultShippingAddress);
            
            stopwatch.Stop();

            // Verify performance
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(PERFORMANCE_THRESHOLD_MS);
            
            // Verify order was created correctly
            order.Should().NotBeNull();
            order.Items.Should().HaveCount(10);
            order.TotalAmount.Amount.Should().Be(250.00m);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task BulkOrderProcessing_ShouldHandleHighVolume()
        {
            // Performance test for bulk order processing
            
            // Arrange
            var customers = new List<Customer>();
            var inventoryItems = new List<InventoryItem>();
            
            for (int i = 0; i < 10; i++)
            {
                customers.Add(await CreateTestCustomerAsync());
                inventoryItems.Add(await CreateTestInventoryItemAsync($"Product {i}", quantity: 10000));
            }

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Create multiple orders concurrently
            var stopwatch = Stopwatch.StartNew();
            
            var orderTasks = Enumerable.Range(0, BULK_OPERATION_SIZE).Select(async i =>
            {
                var customer = customers[i % customers.Count];
                var inventoryItem = inventoryItems[i % inventoryItems.Count];
                
                var orderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        InventoryItemId = inventoryItem.Id,
                        Quantity = 1,
                        UnitPrice = new Money(20.00m + i, "USD")
                    }
                };

                var order = await OrderService.CreateOrderAsync(
                    customer.Id,
                    orderItems,
                    customer.DefaultShippingAddress);

                await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                await OrderService.ProcessOrderAsync(order.Id);
                
                return order;
            });

            var orders = await Task.WhenAll(orderTasks);
            stopwatch.Stop();

            // Assert - Verify performance and correctness
            var averageTimePerOrder = stopwatch.ElapsedMilliseconds / (double)BULK_OPERATION_SIZE;
            averageTimePerOrder.Should().BeLessThan(100); // Average should be under 100ms per order
            
            orders.Should().HaveCount(BULK_OPERATION_SIZE);
            orders.Should().AllSatisfy(o => o.Should().NotBeNull());
            
            // Verify all orders were processed
            var processedOrders = await Context.Orders
                .Where(o => orders.Select(order => order.Id).Contains(o.Id))
                .CountAsync();
            
            processedOrders.Should().Be(BULK_OPERATION_SIZE);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ConcurrentOrderProcessing_ShouldMaintainPerformance()
        {
            // Performance test for concurrent order processing
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 10000);

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Process orders concurrently
            var stopwatch = Stopwatch.StartNew();
            
            var concurrentTasks = Enumerable.Range(0, CONCURRENT_OPERATION_COUNT).Select(async i =>
            {
                var orderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        InventoryItemId = inventoryItem.Id,
                        Quantity = 5,
                        UnitPrice = new Money(30.00m, "USD")
                    }
                };

                var taskStopwatch = Stopwatch.StartNew();
                
                var order = await OrderService.CreateOrderAsync(
                    customer.Id,
                    orderItems,
                    customer.DefaultShippingAddress);

                await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                await OrderService.ProcessOrderAsync(order.Id);
                
                taskStopwatch.Stop();
                
                return new { Order = order, ElapsedMs = taskStopwatch.ElapsedMilliseconds };
            });

            var results = await Task.WhenAll(concurrentTasks);
            stopwatch.Stop();

            // Assert - Verify concurrent performance
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(PERFORMANCE_THRESHOLD_MS * 2);
            
            var maxIndividualTime = results.Max(r => r.ElapsedMs);
            maxIndividualTime.Should().BeLessThan(PERFORMANCE_THRESHOLD_MS);
            
            var averageTime = results.Average(r => r.ElapsedMs);
            averageTime.Should().BeLessThan(500); // Average should be under 500ms
            
            // Verify all orders completed successfully
            results.Should().AllSatisfy(r => r.Order.Should().NotBeNull());
            
            // Verify inventory consistency
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityOnHand.Should().Be(10000 - (CONCURRENT_OPERATION_COUNT * 5));
        }

        #endregion

        #region Inventory Management Performance Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task InventoryUpdates_ShouldHandleHighFrequency()
        {
            // Performance test for high-frequency inventory updates
            
            // Arrange
            var inventoryItems = new List<InventoryItem>();
            
            for (int i = 0; i < 50; i++)
            {
                inventoryItems.Add(await CreateTestInventoryItemAsync($"Product {i}", quantity: 1000));
            }

            // Act - Perform rapid inventory updates
            var stopwatch = Stopwatch.StartNew();
            
            var updateTasks = inventoryItems.Select(async (item, index) =>
            {
                var updates = Enumerable.Range(0, 20).Select(async i =>
                {
                    var newQuantity = 1000 - (i * 10);
                    await InventoryService.UpdateStockAsync(
                        item.Id,
                        newQuantity,
                        $"Update {i} for item {index}");
                });
                
                await Task.WhenAll(updates);
            });

            await Task.WhenAll(updateTasks);
            stopwatch.Stop();

            // Assert - Verify performance
            var totalUpdates = inventoryItems.Count * 20;
            var averageTimePerUpdate = stopwatch.ElapsedMilliseconds / (double)totalUpdates;
            averageTimePerUpdate.Should().BeLessThan(10); // Should be under 10ms per update
            
            // Verify final inventory states
            var finalInventoryItems = await Context.InventoryItems
                .Where(i => inventoryItems.Select(item => item.Id).Contains(i.Id))
                .ToListAsync();
            
            finalInventoryItems.Should().HaveCount(50);
            finalInventoryItems.Should().AllSatisfy(i => i.QuantityOnHand.Should().Be(810)); // 1000 - (19 * 10)
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task InventoryReservationConcurrency_ShouldMaintainConsistency()
        {
            // Performance test for concurrent inventory reservations
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 1000);
            var reservationTasks = new List<Task<bool>>();

            // Act - Attempt concurrent reservations
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                var reservationId = $"reservation-{i}";
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await InventoryService.ReserveInventoryAsync(
                            inventoryItem.Id,
                            5,
                            reservationId);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
                
                reservationTasks.Add(task);
            }

            var results = await Task.WhenAll(reservationTasks);
            stopwatch.Stop();

            // Assert - Verify performance and consistency
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(PERFORMANCE_THRESHOLD_MS);
            
            var successfulReservations = results.Count(r => r);
            successfulReservations.Should().BeLessOrEqualTo(200); // Max 200 items can be reserved (1000/5)
            
            // Verify final inventory state
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityReserved.Should().Be(successfulReservations * 5);
            finalInventory.QuantityAvailable.Should().Be(1000 - (successfulReservations * 5));
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task InventoryLedgerPerformance_ShouldHandleLargeVolume()
        {
            // Performance test for inventory ledger operations
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 10000);
            var ledgerEntries = new List<Task>();

            // Act - Create large volume of ledger entries
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < STRESS_TEST_ITERATIONS; i++)
            {
                var entryType = (InventoryLedgerEntryType)(i % 4); // Cycle through entry types
                var quantity = (i % 2 == 0) ? 1 : -1; // Alternate positive/negative
                
                var task = Task.Run(async () =>
                {
                    var entry = new InventoryLedgerEntry
                    {
                        InventoryItemId = inventoryItem.Id,
                        EntryType = entryType,
                        Quantity = quantity,
                        Reference = $"Performance test entry {i}",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "PerformanceTest"
                    };
                    
                    Context.InventoryLedgerEntries.Add(entry);
                });
                
                ledgerEntries.Add(task);
                
                // Batch save every 100 entries
                if ((i + 1) % 100 == 0)
                {
                    await Task.WhenAll(ledgerEntries);
                    await Context.SaveChangesAsync();
                    ledgerEntries.Clear();
                }
            }
            
            // Save remaining entries
            if (ledgerEntries.Any())
            {
                await Task.WhenAll(ledgerEntries);
                await Context.SaveChangesAsync();
            }
            
            stopwatch.Stop();

            // Assert - Verify performance
            var averageTimePerEntry = stopwatch.ElapsedMilliseconds / (double)STRESS_TEST_ITERATIONS;
            averageTimePerEntry.Should().BeLessThan(5); // Should be under 5ms per entry
            
            // Verify all entries were created
            var totalEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .CountAsync();
            
            totalEntries.Should().BeGreaterOrEqualTo(STRESS_TEST_ITERATIONS);
        }

        #endregion

        #region Payment Processing Performance Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task PaymentProcessing_ShouldHandleHighThroughput()
        {
            // Performance test for payment processing throughput
            
            // Arrange
            var customers = new List<Customer>();
            var orders = new List<Order>();
            
            for (int i = 0; i < 50; i++)
            {
                var customer = await CreateTestCustomerAsync();
                customers.Add(customer);
                
                var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m, "USD"));
                orders.Add(order);
            }

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(() =>
                {
                    Thread.Sleep(10); // Simulate payment gateway latency
                    return CommerceTestUtilities.CreateSuccessfulPaymentResponse();
                });

            // Act - Process payments concurrently
            var stopwatch = Stopwatch.StartNew();
            
            var paymentTasks = orders.Select(async order =>
            {
                var taskStopwatch = Stopwatch.StartNew();
                
                await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                
                taskStopwatch.Stop();
                return taskStopwatch.ElapsedMilliseconds;
            });

            var paymentTimes = await Task.WhenAll(paymentTasks);
            stopwatch.Stop();

            // Assert - Verify performance
            var averagePaymentTime = paymentTimes.Average();
            averagePaymentTime.Should().BeLessThan(200); // Should be under 200ms average
            
            var maxPaymentTime = paymentTimes.Max();
            maxPaymentTime.Should().BeLessThan(500); // No payment should take more than 500ms
            
            // Verify all payments were processed
            var processedPayments = await Context.Payments
                .Where(p => orders.Select(o => o.Id).Contains(p.OrderId))
                .CountAsync();
            
            processedPayments.Should().Be(50);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task PaymentRetryPerformance_ShouldHandleFailuresEfficiently()
        {
            // Performance test for payment retry mechanism
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var orders = new List<Order>();
            
            for (int i = 0; i < 20; i++)
            {
                orders.Add(await CreateTestOrderAsync(customer.Id, new Money(50.00m, "USD")));
            }

            var attemptCounts = new ConcurrentDictionary<Guid, int>();
            
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync((PaymentRequest request) =>
                {
                    var orderId = request.OrderId;
                    var attempts = attemptCounts.AddOrUpdate(orderId, 1, (key, value) => value + 1);
                    
                    // Fail first 2 attempts, succeed on 3rd
                    if (attempts <= 2)
                    {
                        Thread.Sleep(50); // Simulate network delay
                        return CommerceTestUtilities.CreateFailedPaymentResponse("Temporary failure");
                    }
                    
                    Thread.Sleep(20); // Simulate successful processing
                    return CommerceTestUtilities.CreateSuccessfulPaymentResponse();
                });

            // Act - Process payments with retry logic
            var stopwatch = Stopwatch.StartNew();
            
            var retryTasks = orders.Select(async order =>
            {
                var maxRetries = 3;
                var retryCount = 0;
                var success = false;
                
                while (retryCount < maxRetries && !success)
                {
                    try
                    {
                        await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                        success = true;
                    }
                    catch (PaymentProcessingException)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(100 * retryCount); // Exponential backoff
                        }
                    }
                }
                
                return success;
            });

            var results = await Task.WhenAll(retryTasks);
            stopwatch.Stop();

            // Assert - Verify performance
            var averageTimePerPayment = stopwatch.ElapsedMilliseconds / (double)orders.Count;
            averageTimePerPayment.Should().BeLessThan(500); // Should be under 500ms per payment including retries
            
            // Verify all payments eventually succeeded
            results.Should().AllSatisfy(r => r.Should().BeTrue());
            
            // Verify retry attempts were recorded
            var totalAttempts = attemptCounts.Values.Sum();
            totalAttempts.Should().Be(orders.Count * 3); // Each order should have exactly 3 attempts
        }

        #endregion

        #region Subscription Performance Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task SubscriptionRenewalBatch_ShouldProcessEfficiently()
        {
            // Performance test for batch subscription renewal processing
            
            // Arrange
            var customers = new List<Customer>();
            var subscriptions = new List<Subscription>();
            
            var plan = await CreateTestSubscriptionPlanAsync(
                "Performance Plan",
                new Money(19.99m, "USD"),
                BillingCycle.Monthly);
            
            for (int i = 0; i < BULK_OPERATION_SIZE; i++)
            {
                var customer = await CreateTestCustomerAsync();
                customers.Add(customer);
                
                var subscription = await SubscriptionService.CreateSubscriptionAsync(
                    customer.Id,
                    plan.Id,
                    DateTime.UtcNow.AddDays(-30)); // Due for renewal
                
                subscriptions.Add(subscription);
            }

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(() =>
                {
                    Thread.Sleep(5); // Simulate payment processing
                    return CommerceTestUtilities.CreateSuccessfulPaymentResponse();
                });

            // Act - Process batch renewals
            var stopwatch = Stopwatch.StartNew();
            
            var renewalTasks = subscriptions.Select(async subscription =>
            {
                return await SubscriptionService.ProcessRenewalAsync(
                    subscription.Id,
                    DateTime.UtcNow);
            });

            var renewalResults = await Task.WhenAll(renewalTasks);
            stopwatch.Stop();

            // Assert - Verify performance
            var averageTimePerRenewal = stopwatch.ElapsedMilliseconds / (double)BULK_OPERATION_SIZE;
            averageTimePerRenewal.Should().BeLessThan(100); // Should be under 100ms per renewal
            
            // Verify all renewals succeeded
            renewalResults.Should().AllSatisfy(r => r.Should().BeTrue());
            
            // Verify renewal payments were created
            var renewalPayments = await Context.Payments
                .Where(p => subscriptions.Select(s => s.Id).Contains(p.SubscriptionId.Value))
                .CountAsync();
            
            renewalPayments.Should().Be(BULK_OPERATION_SIZE);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task SubscriptionStatusUpdates_ShouldHandleHighVolume()
        {
            // Performance test for subscription status updates
            
            // Arrange
            var subscriptions = new List<Subscription>();
            var plan = await CreateTestSubscriptionPlanAsync(
                "Status Test Plan",
                new Money(9.99m, "USD"),
                BillingCycle.Monthly);
            
            for (int i = 0; i < 200; i++)
            {
                var customer = await CreateTestCustomerAsync();
                var subscription = await SubscriptionService.CreateSubscriptionAsync(
                    customer.Id,
                    plan.Id,
                    DateTime.UtcNow);
                
                subscriptions.Add(subscription);
            }

            // Act - Update subscription statuses concurrently
            var stopwatch = Stopwatch.StartNew();
            
            var updateTasks = subscriptions.Select(async (subscription, index) =>
            {
                var newStatus = (index % 3) switch
                {
                    0 => SubscriptionStatus.Active,
                    1 => SubscriptionStatus.Paused,
                    _ => SubscriptionStatus.Cancelled
                };
                
                if (newStatus == SubscriptionStatus.Active)
                {
                    await SubscriptionService.ActivateSubscriptionAsync(subscription.Id);
                }
                else if (newStatus == SubscriptionStatus.Paused)
                {
                    await SubscriptionService.PauseSubscriptionAsync(subscription.Id, "Performance test");
                }
                else
                {
                    await SubscriptionService.CancelSubscriptionAsync(
                        subscription.Id,
                        DateTime.UtcNow,
                        "Performance test");
                }
            });

            await Task.WhenAll(updateTasks);
            stopwatch.Stop();

            // Assert - Verify performance
            var averageTimePerUpdate = stopwatch.ElapsedMilliseconds / (double)subscriptions.Count;
            averageTimePerUpdate.Should().BeLessThan(50); // Should be under 50ms per update
            
            // Verify status distributions
            var finalSubscriptions = await Context.Subscriptions
                .Where(s => subscriptions.Select(sub => sub.Id).Contains(s.Id))
                .ToListAsync();
            
            var activeCount = finalSubscriptions.Count(s => s.Status == SubscriptionStatus.Active);
            var pausedCount = finalSubscriptions.Count(s => s.Status == SubscriptionStatus.Paused);
            var cancelledCount = finalSubscriptions.Count(s => s.Status == SubscriptionStatus.Cancelled);
            
            // Verify roughly equal distribution
            activeCount.Should().BeInRange(60, 80);
            pausedCount.Should().BeInRange(60, 80);
            cancelledCount.Should().BeInRange(60, 80);
        }

        #endregion

        #region Database Performance Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Database)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task DatabaseQueryPerformance_ShouldMeetThresholds()
        {
            // Performance test for database query operations
            
            // Arrange - Create test data
            var customers = new List<Customer>();
            var orders = new List<Order>();
            
            for (int i = 0; i < 100; i++)
            {
                var customer = await CreateTestCustomerAsync();
                customers.Add(customer);
                
                for (int j = 0; j < 5; j++)
                {
                    var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m + j, "USD"));
                    orders.Add(order);
                }
            }

            // Act & Assert - Test various query patterns
            
            // Test 1: Customer lookup by ID
            var customerLookupStopwatch = Stopwatch.StartNew();
            
            var customerLookupTasks = customers.Take(50).Select(async customer =>
            {
                return await Context.Customers.FindAsync(customer.Id);
            });
            
            await Task.WhenAll(customerLookupTasks);
            customerLookupStopwatch.Stop();
            
            var avgCustomerLookupTime = customerLookupStopwatch.ElapsedMilliseconds / 50.0;
            avgCustomerLookupTime.Should().BeLessThan(10); // Should be under 10ms per lookup
            
            // Test 2: Order queries with includes
            var orderQueryStopwatch = Stopwatch.StartNew();
            
            var orderQueries = customers.Take(20).Select(async customer =>
            {
                return await Context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.Payments)
                    .Where(o => o.CustomerId == customer.Id)
                    .ToListAsync();
            });
            
            await Task.WhenAll(orderQueries);
            orderQueryStopwatch.Stop();
            
            var avgOrderQueryTime = orderQueryStopwatch.ElapsedMilliseconds / 20.0;
            avgOrderQueryTime.Should().BeLessThan(50); // Should be under 50ms per complex query
            
            // Test 3: Aggregate queries
            var aggregateStopwatch = Stopwatch.StartNew();
            
            var totalOrderValue = await Context.Orders
                .Where(o => customers.Select(c => c.Id).Contains(o.CustomerId))
                .SumAsync(o => o.TotalAmount.Amount);
            
            var customerOrderCounts = await Context.Orders
                .Where(o => customers.Select(c => c.Id).Contains(o.CustomerId))
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            aggregateStopwatch.Stop();
            
            aggregateStopwatch.ElapsedMilliseconds.Should().BeLessThan(200); // Aggregates under 200ms
            
            // Verify results
            totalOrderValue.Should().BeGreaterThan(0);
            customerOrderCounts.Should().HaveCount(100);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Database)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task DatabaseConcurrentAccess_ShouldMaintainPerformance()
        {
            // Performance test for concurrent database access
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 10000);
            var concurrentOperations = new List<Task>();

            // Act - Perform concurrent database operations
            var stopwatch = Stopwatch.StartNew();
            
            // Concurrent reads
            for (int i = 0; i < 50; i++)
            {
                concurrentOperations.Add(Task.Run(async () =>
                {
                    var item = await Context.InventoryItems
                        .Include(i => i.LedgerEntries)
                        .FirstOrDefaultAsync(i => i.Id == inventoryItem.Id);
                    
                    return item;
                }));
            }
            
            // Concurrent writes
            for (int i = 0; i < 20; i++)
            {
                var entryIndex = i;
                concurrentOperations.Add(Task.Run(async () =>
                {
                    var entry = new InventoryLedgerEntry
                    {
                        InventoryItemId = inventoryItem.Id,
                        EntryType = InventoryLedgerEntryType.Adjustment,
                        Quantity = 1,
                        Reference = $"Concurrent test {entryIndex}",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "ConcurrentTest"
                    };
                    
                    Context.InventoryLedgerEntries.Add(entry);
                    await Context.SaveChangesAsync();
                }));
            }
            
            await Task.WhenAll(concurrentOperations);
            stopwatch.Stop();

            // Assert - Verify performance
            var averageOperationTime = stopwatch.ElapsedMilliseconds / (double)concurrentOperations.Count;
            averageOperationTime.Should().BeLessThan(100); // Should be under 100ms per operation
            
            // Verify data integrity
            var ledgerEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .Where(e => e.Reference.StartsWith("Concurrent test"))
                .CountAsync();
            
            ledgerEntries.Should().Be(20); // All concurrent writes should succeed
        }

        #endregion

        #region Memory and Resource Performance Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Performance)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task MemoryUsage_ShouldRemainWithinLimits()
        {
            // Performance test for memory usage during operations
            
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var customers = new List<Customer>();
            var orders = new List<Order>();

            // Act - Perform memory-intensive operations
            for (int i = 0; i < 200; i++)
            {
                var customer = await CreateTestCustomerAsync();
                customers.Add(customer);
                
                var order = await CreateTestOrderAsync(customer.Id, new Money(50.00m, "USD"));
                orders.Add(order);
                
                // Force garbage collection every 50 iterations
                if ((i + 1) % 50 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            
            // Process all orders
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());
            
            var processingTasks = orders.Select(async order =>
            {
                await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                await OrderService.ProcessOrderAsync(order.Id);
            });
            
            await Task.WhenAll(processingTasks);
            
            // Force final garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);

            // Assert - Verify memory usage
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreasePerOrder = memoryIncrease / (double)orders.Count;
            
            // Memory increase should be reasonable (less than 50KB per order)
            memoryIncreasePerOrder.Should().BeLessThan(50 * 1024);
            
            // Total memory increase should be less than 50MB
            memoryIncrease.Should().BeLessThan(50 * 1024 * 1024);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Performance)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task ResourceCleanup_ShouldReleaseProperlyUnderLoad()
        {
            // Performance test for resource cleanup under load
            
            // Arrange
            var iterations = 100;
            var resourceTasks = new List<Task>();

            // Act - Create and dispose resources rapidly
            for (int i = 0; i < iterations; i++)
            {
                resourceTasks.Add(Task.Run(async () =>
                {
                    // Create temporary resources
                    var customer = await CreateTestCustomerAsync();
                    var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
                    
                    var orderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            InventoryItemId = inventoryItem.Id,
                            Quantity = 1,
                            UnitPrice = new Money(25.00m, "USD")
                        }
                    };
                    
                    var order = await OrderService.CreateOrderAsync(
                        customer.Id,
                        orderItems,
                        customer.DefaultShippingAddress);
                    
                    // Simulate resource usage
                    await Task.Delay(10);
                    
                    return order.Id;
                }));
            }
            
            var orderIds = await Task.WhenAll(resourceTasks);
            
            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Assert - Verify resources were created and can be accessed
            var createdOrders = await Context.Orders
                .Where(o => orderIds.Contains(o.Id))
                .CountAsync();
            
            createdOrders.Should().Be(iterations);
            
            // Verify no resource leaks by checking entity tracking
            var trackedEntities = Context.ChangeTracker.Entries().Count();
            trackedEntities.Should().BeLessThan(1000); // Should not be tracking excessive entities
        }

        #endregion

        #region Stress Testing

        [Fact(Skip = "Long running stress test - enable manually")]
        [Trait(TestTraits.Feature, CommerceFeatures.Performance)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task StressTest_ShouldHandleExtremLoad()
        {
            // Extreme stress test for system limits
            
            // Arrange
            var stressTestDuration = TimeSpan.FromMinutes(2);
            var operationsPerSecond = 50;
            var totalOperations = (int)(stressTestDuration.TotalSeconds * operationsPerSecond);
            
            var customers = new List<Customer>();
            var inventoryItems = new List<InventoryItem>();
            
            // Pre-create test data
            for (int i = 0; i < 20; i++)
            {
                customers.Add(await CreateTestCustomerAsync());
                inventoryItems.Add(await CreateTestInventoryItemAsync($"Stress Product {i}", quantity: 100000));
            }

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            var successfulOperations = 0;
            var failedOperations = 0;
            var operationTimes = new ConcurrentBag<long>();

            // Act - Run stress test
            var stopwatch = Stopwatch.StartNew();
            var semaphore = new SemaphoreSlim(20); // Limit concurrent operations
            
            var stressTasks = Enumerable.Range(0, totalOperations).Select(async i =>
            {
                await semaphore.WaitAsync();
                
                try
                {
                    var operationStopwatch = Stopwatch.StartNew();
                    
                    var customer = customers[i % customers.Count];
                    var inventoryItem = inventoryItems[i % inventoryItems.Count];
                    
                    var orderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            InventoryItemId = inventoryItem.Id,
                            Quantity = 1,
                            UnitPrice = new Money(25.00m, "USD")
                        }
                    };
                    
                    var order = await OrderService.CreateOrderAsync(
                        customer.Id,
                        orderItems,
                        customer.DefaultShippingAddress);
                    
                    await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                    await OrderService.ProcessOrderAsync(order.Id);
                    
                    operationStopwatch.Stop();
                    operationTimes.Add(operationStopwatch.ElapsedMilliseconds);
                    
                    Interlocked.Increment(ref successfulOperations);
                }
                catch
                {
                    Interlocked.Increment(ref failedOperations);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            await Task.WhenAll(stressTasks);
            stopwatch.Stop();

            // Assert - Verify stress test results
            var successRate = (double)successfulOperations / totalOperations;
            successRate.Should().BeGreaterThan(0.95); // At least 95% success rate
            
            var averageOperationTime = operationTimes.Average();
            averageOperationTime.Should().BeLessThan(1000); // Average under 1 second
            
            var actualThroughput = successfulOperations / stopwatch.Elapsed.TotalSeconds;
            actualThroughput.Should().BeGreaterThan(operationsPerSecond * 0.8); // At least 80% of target throughput
            
            // Log stress test results
            var maxOperationTime = operationTimes.Max();
            var minOperationTime = operationTimes.Min();
            
            // These are informational assertions
            maxOperationTime.Should().BeLessThan(5000); // No operation should take more than 5 seconds
            minOperationTime.Should().BeGreaterThan(0); // All operations should take some time
        }

        #endregion
    }
}