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
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using NBomber.Contracts;
using NBomber.CSharp;

namespace CoreAxis.Modules.CommerceModule.Tests.Stress
{
    /// <summary>
    /// Stress tests for Commerce Module
    /// Tests system behavior under extreme load conditions
    /// </summary>
    [Trait(TestTraits.Category, TestCategories.Stress)]
    public class CommerceStressTests : CommerceTestBase
    {
        private const int STRESS_TIMEOUT_MS = 120000; // 2 minutes
        private const int EXTREME_LOAD_SIZE = 5000;
        private const int HIGH_CONCURRENCY = 500;
        private const int MEMORY_PRESSURE_ITERATIONS = 100;

        #region Order Processing Stress Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task ExtremeOrderVolume_ShouldHandleGracefully()
        {
            // Arrange
            var customers = new List<Customer>();
            var inventoryItems = new List<InventoryItem>();
            
            // Create test data
            for (int i = 0; i < 100; i++)
            {
                customers.Add(await CreateTestCustomerAsync());
                inventoryItems.Add(await CreateTestInventoryItemAsync(quantity: 100000));
            }

            var orderCount = EXTREME_LOAD_SIZE;
            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            
            var stopwatch = Stopwatch.StartNew();

            // Act - Create extreme number of orders
            var orderTasks = Enumerable.Range(0, orderCount).Select(async i =>
            {
                try
                {
                    var customer = customers[i % customers.Count];
                    var inventoryItem = inventoryItems[i % inventoryItems.Count];
                    
                    var orderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            InventoryItemId = inventoryItem.Id,
                            Quantity = 1,
                            UnitPrice = new Money(10.00m + (i % 50), "USD")
                        }
                    };

                    var order = await OrderService.CreateOrderAsync(
                        customer.Id, 
                        orderItems, 
                        customer.DefaultShippingAddress);
                    
                    lock (lockObject)
                    {
                        successCount++;
                    }
                    
                    return true;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failureCount++;
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(orderTasks);
            stopwatch.Stop();

            // Assert
            (successCount + failureCount).Should().Be(orderCount);
            
            // Under extreme load, we expect some failures but system should remain stable
            var successRate = (double)successCount / orderCount;
            successRate.Should().BeGreaterThan(0.7); // At least 70% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
            
            // Verify system stability - no memory leaks or deadlocks
            GC.Collect();
            var memoryAfter = GC.GetTotalMemory(false);
            (memoryAfter / 1024 / 1024).Should().BeLessThan(1000); // Less than 1GB
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task HighConcurrencyOrderProcessing_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100000);
            
            var orders = new List<Order>();
            for (int i = 0; i < HIGH_CONCURRENCY; i++)
            {
                var orderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        InventoryItemId = inventoryItem.Id,
                        Quantity = 10,
                        UnitPrice = new Money(25.00m, "USD")
                    }
                };

                var order = await OrderService.CreateOrderAsync(
                    customer.Id, 
                    orderItems, 
                    customer.DefaultShippingAddress);
                orders.Add(order);
            }

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();

            // Act - Process orders with high concurrency
            var processingTasks = orders.Select(async order =>
            {
                try
                {
                    await OrderService.ProcessOrderAsync(order.Id);
                    
                    lock (lockObject)
                    {
                        successCount++;
                    }
                    return true;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failureCount++;
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(processingTasks);
            stopwatch.Stop();

            // Assert
            (successCount + failureCount).Should().Be(HIGH_CONCURRENCY);
            
            // High concurrency should still maintain reasonable success rate
            var successRate = (double)successCount / HIGH_CONCURRENCY;
            successRate.Should().BeGreaterThan(0.8); // At least 80% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
            
            // Verify data integrity
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityOnHand.Should().BeGreaterOrEqualTo(0);
            finalInventory.QuantityReserved.Should().BeGreaterOrEqualTo(0);
            finalInventory.QuantityAvailable.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task ContinuousOrderCreation_ShouldNotDegradePerformance()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 50000);
            
            var batchSize = 100;
            var batchCount = 20;
            var performanceMetrics = new List<double>();

            // Act - Create orders in batches and measure performance degradation
            for (int batch = 0; batch < batchCount; batch++)
            {
                var batchStopwatch = Stopwatch.StartNew();
                
                var batchTasks = Enumerable.Range(0, batchSize).Select(async i =>
                {
                    var orderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            InventoryItemId = inventoryItem.Id,
                            Quantity = 1,
                            UnitPrice = new Money(15.00m, "USD")
                        }
                    };

                    return await OrderService.CreateOrderAsync(
                        customer.Id, 
                        orderItems, 
                        customer.DefaultShippingAddress);
                });

                await Task.WhenAll(batchTasks);
                batchStopwatch.Stop();
                
                var averageTimePerOrder = batchStopwatch.ElapsedMilliseconds / (double)batchSize;
                performanceMetrics.Add(averageTimePerOrder);
                
                // Small delay between batches to simulate real-world usage
                await Task.Delay(100);
            }

            // Assert
            performanceMetrics.Should().HaveCount(batchCount);
            
            // Performance should not degrade significantly over time
            var firstBatchAverage = performanceMetrics.Take(3).Average();
            var lastBatchAverage = performanceMetrics.TakeLast(3).Average();
            
            // Last batches should not be more than 50% slower than first batches
            (lastBatchAverage / firstBatchAverage).Should().BeLessThan(1.5);
            
            // All batches should maintain reasonable performance
            performanceMetrics.All(m => m < 100).Should().BeTrue(); // Less than 100ms per order
        }

        #endregion

        #region Inventory Stress Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task ExtremeInventoryContention_ShouldPreventOverselling()
        {
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 1000);
            var concurrentReservations = 2000; // More reservations than available stock
            var reservationQuantity = 1;
            
            var successfulReservations = 0;
            var failedReservations = 0;
            var lockObject = new object();
            
            var stopwatch = Stopwatch.StartNew();

            // Act - Attempt many concurrent reservations
            var reservationTasks = Enumerable.Range(0, concurrentReservations).Select(async i =>
            {
                try
                {
                    var success = await InventoryService.ReserveAsync(
                        inventoryItem.Id, 
                        reservationQuantity, 
                        $"Stress test reservation {i}");
                    
                    lock (lockObject)
                    {
                        if (success)
                            successfulReservations++;
                        else
                            failedReservations++;
                    }
                    
                    return success;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failedReservations++;
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(reservationTasks);
            stopwatch.Stop();

            // Assert
            (successfulReservations + failedReservations).Should().Be(concurrentReservations);
            
            // Should not oversell - successful reservations should not exceed available stock
            successfulReservations.Should().BeLessOrEqualTo(1000);
            
            // Verify final inventory state
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityReserved.Should().Be(successfulReservations * reservationQuantity);
            finalInventory.QuantityAvailable.Should().Be(1000 - (successfulReservations * reservationQuantity));
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task RapidInventoryUpdates_ShouldMaintainConsistency()
        {
            // Arrange
            var inventoryItems = new List<InventoryItem>();
            for (int i = 0; i < 50; i++)
            {
                inventoryItems.Add(await CreateTestInventoryItemAsync(quantity: 10000));
            }

            var operationsPerItem = 200;
            var totalOperations = inventoryItems.Count * operationsPerItem;
            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            
            var stopwatch = Stopwatch.StartNew();

            // Act - Perform rapid updates across multiple inventory items
            var updateTasks = inventoryItems.SelectMany(item =>
                Enumerable.Range(0, operationsPerItem).Select(async i =>
                {
                    try
                    {
                        var operation = i % 4;
                        var quantity = new Random(i).Next(1, 20);
                        
                        switch (operation)
                        {
                            case 0: // Reserve
                                await InventoryService.ReserveAsync(
                                    item.Id, quantity, $"Rapid test {i}");
                                break;
                            case 1: // Release
                                await InventoryService.ReleaseReservationAsync(
                                    item.Id, quantity, $"Rapid test {i}");
                                break;
                            case 2: // Update stock
                                var currentItem = await Context.InventoryItems.FindAsync(item.Id);
                                if (currentItem != null)
                                {
                                    await InventoryService.UpdateStockAsync(
                                        item.Id, 
                                        Math.Max(0, currentItem.QuantityOnHand + quantity - 10), 
                                        $"Rapid test {i}");
                                }
                                break;
                            case 3: // Check availability
                                await InventoryService.CheckAvailabilityAsync(item.Id, quantity);
                                break;
                        }
                        
                        lock (lockObject)
                        {
                            successCount++;
                        }
                        return true;
                    }
                    catch
                    {
                        lock (lockObject)
                        {
                            failureCount++;
                        }
                        return false;
                    }
                })
            );

            var results = await Task.WhenAll(updateTasks);
            stopwatch.Stop();

            // Assert
            (successCount + failureCount).Should().Be(totalOperations);
            
            // Under stress, we expect some failures but majority should succeed
            var successRate = (double)successCount / totalOperations;
            successRate.Should().BeGreaterThan(0.7); // At least 70% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
            
            // Verify data consistency for all items
            foreach (var item in inventoryItems)
            {
                var finalItem = await Context.InventoryItems.FindAsync(item.Id);
                finalItem.QuantityOnHand.Should().BeGreaterOrEqualTo(0);
                finalItem.QuantityReserved.Should().BeGreaterOrEqualTo(0);
                finalItem.QuantityAvailable.Should().BeGreaterOrEqualTo(0);
                (finalItem.QuantityOnHand - finalItem.QuantityReserved)
                    .Should().Be(finalItem.QuantityAvailable);
            }
        }

        #endregion

        #region Payment Processing Stress Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task HighVolumePaymentProcessing_ShouldHandleLoad()
        {
            // Arrange
            var customers = new List<Customer>();
            var orders = new List<Order>();
            
            for (int i = 0; i < 1000; i++)
            {
                var customer = await CreateTestCustomerAsync();
                customers.Add(customer);
                
                var order = await CreateTestOrderAsync(customer.Id, new Money(50.00m + (i % 100), "USD"));
                orders.Add(order);
            }

            // Simulate payment provider with some failures
            var paymentCallCount = 0;
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(() =>
                {
                    var count = Interlocked.Increment(ref paymentCallCount);
                    // Simulate 10% failure rate
                    if (count % 10 == 0)
                    {
                        return CommerceTestUtilities.CreateFailedPaymentResponse("Simulated payment failure");
                    }
                    return CommerceTestUtilities.CreateSuccessfulPaymentResponse();
                });

            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();

            // Act - Process payments with high volume
            var paymentTasks = orders.Select(async order =>
            {
                try
                {
                    await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                    
                    lock (lockObject)
                    {
                        successCount++;
                    }
                    return true;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failureCount++;
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(paymentTasks);
            stopwatch.Stop();

            // Assert
            (successCount + failureCount).Should().Be(1000);
            
            // Should handle the expected failure rate gracefully
            var successRate = (double)successCount / 1000;
            successRate.Should().BeGreaterThan(0.85); // At least 85% success rate (accounting for simulated failures)
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task ConcurrentRefundProcessing_ShouldMaintainIntegrity()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var orders = new List<Order>();
            
            // Create and complete orders first
            for (int i = 0; i < 200; i++)
            {
                var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m, "USD"));
                orders.Add(order);
                
                MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                    .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());
                
                await OrderService.ProcessOrderAsync(order.Id);
                await OrderService.CompleteOrderAsync(order.Id);
            }

            MockPaymentService.Setup(x => x.ProcessRefundAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulRefundResponse());

            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();

            // Act - Process refunds concurrently
            var refundTasks = orders.Select(async order =>
            {
                try
                {
                    var refundRequest = await RefundService.CreateRefundRequestAsync(
                        order.Id, 
                        new Money(50.00m, "USD"), 
                        "Stress test refund");
                    
                    await RefundService.ProcessRefundAsync(refundRequest.Id);
                    
                    lock (lockObject)
                    {
                        successCount++;
                    }
                    return true;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failureCount++;
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(refundTasks);
            stopwatch.Stop();

            // Assert
            (successCount + failureCount).Should().Be(200);
            
            var successRate = (double)successCount / 200;
            successRate.Should().BeGreaterThan(0.9); // At least 90% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
        }

        #endregion

        #region Database Stress Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Database)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task HighConnectionLoad_ShouldNotExhaustPool()
        {
            // Arrange
            var connectionCount = 200;
            var operationsPerConnection = 10;
            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            
            var stopwatch = Stopwatch.StartNew();

            // Act - Create many concurrent database operations
            var connectionTasks = Enumerable.Range(0, connectionCount).Select(async i =>
            {
                var operationTasks = Enumerable.Range(0, operationsPerConnection).Select(async j =>
                {
                    try
                    {
                        var operation = j % 4;
                        
                        switch (operation)
                        {
                            case 0: // Query customers
                                await Context.Customers.Take(5).ToListAsync();
                                break;
                            case 1: // Query orders
                                await Context.Orders.Take(5).ToListAsync();
                                break;
                            case 2: // Query inventory
                                await Context.InventoryItems.Take(5).ToListAsync();
                                break;
                            case 3: // Complex query
                                await Context.Orders
                                    .Include(o => o.Items)
                                    .Where(o => o.TotalAmount.Amount > 50)
                                    .Take(3)
                                    .ToListAsync();
                                break;
                        }
                        
                        lock (lockObject)
                        {
                            successCount++;
                        }
                        return true;
                    }
                    catch
                    {
                        lock (lockObject)
                        {
                            failureCount++;
                        }
                        return false;
                    }
                });
                
                await Task.WhenAll(operationTasks);
            });

            await Task.WhenAll(connectionTasks);
            stopwatch.Stop();

            // Assert
            var totalOperations = connectionCount * operationsPerConnection;
            (successCount + failureCount).Should().Be(totalOperations);
            
            // Should handle high connection load without exhausting pool
            var successRate = (double)successCount / totalOperations;
            successRate.Should().BeGreaterThan(0.95); // At least 95% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Database)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task LongRunningTransactions_ShouldNotCauseDeadlocks()
        {
            // Arrange
            var customers = new List<Customer>();
            var inventoryItems = new List<InventoryItem>();
            
            for (int i = 0; i < 20; i++)
            {
                customers.Add(await CreateTestCustomerAsync());
                inventoryItems.Add(await CreateTestInventoryItemAsync(quantity: 5000));
            }

            var transactionCount = 100;
            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();
            var stopwatch = Stopwatch.StartNew();

            // Act - Perform long-running transactions that could potentially deadlock
            var transactionTasks = Enumerable.Range(0, transactionCount).Select(async i =>
            {
                try
                {
                    using var transaction = await Context.Database.BeginTransactionAsync();
                    
                    // Simulate long-running transaction with multiple operations
                    var customer = customers[i % customers.Count];
                    var inventoryItem = inventoryItems[i % inventoryItems.Count];
                    
                    // Create order
                    var orderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            InventoryItemId = inventoryItem.Id,
                            Quantity = 5,
                            UnitPrice = new Money(20.00m, "USD")
                        }
                    };

                    var order = await OrderService.CreateOrderAsync(
                        customer.Id, 
                        orderItems, 
                        customer.DefaultShippingAddress);
                    
                    // Reserve inventory
                    await InventoryService.ReserveAsync(
                        inventoryItem.Id, 5, $"Long transaction {i}");
                    
                    // Simulate processing time
                    await Task.Delay(50);
                    
                    // Update inventory
                    var currentItem = await Context.InventoryItems.FindAsync(inventoryItem.Id);
                    if (currentItem != null)
                    {
                        await InventoryService.UpdateStockAsync(
                            inventoryItem.Id, 
                            currentItem.QuantityOnHand - 1, 
                            $"Long transaction update {i}");
                    }
                    
                    await transaction.CommitAsync();
                    
                    lock (lockObject)
                    {
                        successCount++;
                    }
                    return true;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failureCount++;
                    }
                    return false;
                }
            });

            var results = await Task.WhenAll(transactionTasks);
            stopwatch.Stop();

            // Assert
            (successCount + failureCount).Should().Be(transactionCount);
            
            // Should handle long-running transactions without deadlocks
            var successRate = (double)successCount / transactionCount;
            successRate.Should().BeGreaterThan(0.8); // At least 80% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
        }

        #endregion

        #region Memory and Resource Stress Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Memory)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task MemoryPressureTest_ShouldNotCauseOutOfMemory()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var customer = await CreateTestCustomerAsync();
            var memorySnapshots = new List<long>();
            
            // Act - Create memory pressure through large operations
            for (int iteration = 0; iteration < MEMORY_PRESSURE_ITERATIONS; iteration++)
            {
                var largeBatchTasks = Enumerable.Range(0, 100).Select(async i =>
                {
                    // Create large orders with many items
                    var inventoryItems = new List<InventoryItem>();
                    for (int j = 0; j < 20; j++)
                    {
                        inventoryItems.Add(await CreateTestInventoryItemAsync(quantity: 1000));
                    }

                    var orderItems = inventoryItems.Select(item => new OrderItem
                    {
                        InventoryItemId = item.Id,
                        Quantity = 2,
                        UnitPrice = new Money(15.00m, "USD")
                    }).ToList();

                    var order = await OrderService.CreateOrderAsync(
                        customer.Id, 
                        orderItems, 
                        customer.DefaultShippingAddress);
                    
                    return order;
                });

                await Task.WhenAll(largeBatchTasks);
                
                // Force garbage collection and measure memory
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                
                // Small delay to allow cleanup
                await Task.Delay(100);
            }

            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            memorySnapshots.Should().HaveCount(MEMORY_PRESSURE_ITERATIONS);
            
            // Memory should not grow unbounded
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseMB = memoryIncrease / 1024 / 1024;
            
            // Memory increase should be reasonable (less than 500MB)
            memoryIncreaseMB.Should().BeLessThan(500);
            
            // Memory usage should stabilize (not continuously growing)
            var lastFiveSnapshots = memorySnapshots.TakeLast(5).ToList();
            var memoryVariance = lastFiveSnapshots.Max() - lastFiveSnapshots.Min();
            var memoryVarianceMB = memoryVariance / 1024 / 1024;
            
            // Memory variance in last 5 snapshots should be reasonable
            memoryVarianceMB.Should().BeLessThan(100);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Threading)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task ThreadPoolExhaustion_ShouldNotCauseDeadlock()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100000);
            
            var taskCount = 1000; // Large number of tasks to stress thread pool
            var completedTasks = 0;
            var lockObject = new object();
            
            var stopwatch = Stopwatch.StartNew();

            // Act - Create many tasks that could exhaust thread pool
            var tasks = Enumerable.Range(0, taskCount).Select(async i =>
            {
                try
                {
                    // Mix of CPU-bound and I/O-bound operations
                    if (i % 2 == 0)
                    {
                        // I/O-bound: Database operation
                        await InventoryService.CheckAvailabilityAsync(inventoryItem.Id, 1);
                    }
                    else
                    {
                        // CPU-bound: Calculation
                        await Task.Run(() =>
                        {
                            var result = 0;
                            for (int j = 0; j < 10000; j++)
                            {
                                result += j * j;
                            }
                            return result;
                        });
                    }
                    
                    // Simulate some processing time
                    await Task.Delay(10);
                    
                    lock (lockObject)
                    {
                        completedTasks++;
                    }
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            completedTasks.Should().Be(taskCount);
            
            // Should complete without deadlock
            var successfulTasks = results.Count(r => r);
            successfulTasks.Should().BeGreaterThan(taskCount * 0.95); // At least 95% success rate
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(STRESS_TIMEOUT_MS);
        }

        #endregion

        #region NBomber Load Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.LoadTesting)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task NBomberOrderCreationLoadTest_ShouldHandleTargetThroughput()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100000);
            
            var scenario = Scenario.Create("order_creation_load", async context =>
            {
                try
                {
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
                    
                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30))
            );

            // Act
            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportFolder("load_test_reports")
                .Run();

            // Assert
            stats.AllOkCount.Should().BeGreaterThan(250); // At least 250 successful requests
            stats.AllFailCount.Should().BeLessThan(50); // Less than 50 failed requests
            stats.ScenarioStats[0].Ok.Mean.Should().BeLessThan(1000); // Average response time < 1s
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.LoadTesting)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task NBomberInventoryReservationLoadTest_ShouldMaintainConsistency()
        {
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 10000);
            
            var scenario = Scenario.Create("inventory_reservation_load", async context =>
            {
                try
                {
                    var success = await InventoryService.ReserveAsync(
                        inventoryItem.Id, 
                        1, 
                        $"Load test reservation {context.InvocationNumber}");
                    
                    return success ? Response.Ok() : Response.Fail("Reservation failed");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message);
                }
            })
            .WithLoadSimulations(
                Simulation.InjectPerSec(rate: 20, during: TimeSpan.FromSeconds(20))
            );

            // Act
            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportFolder("load_test_reports")
                .Run();

            // Assert
            stats.AllOkCount.Should().BeGreaterThan(300); // At least 300 successful reservations
            stats.ScenarioStats[0].Ok.Mean.Should().BeLessThan(500); // Average response time < 500ms
            
            // Verify inventory consistency
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityReserved.Should().Be(stats.AllOkCount);
            finalInventory.QuantityAvailable.Should().Be(10000 - stats.AllOkCount);
        }

        #endregion

        #region Cleanup and Utilities

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Aggressive cleanup for stress tests
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            
            base.Dispose(disposing);
        }

        #endregion
    }
}