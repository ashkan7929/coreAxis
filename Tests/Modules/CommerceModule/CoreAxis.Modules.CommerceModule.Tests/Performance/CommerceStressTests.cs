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

public class CommerceStressTests : IClassFixture<CommerceTestFixture>
{
    private readonly CommerceTestFixture _fixture;
    private readonly ILogger<CommerceStressTests> _logger;

    public CommerceStressTests(CommerceTestFixture fixture)
    {
        _fixture = fixture;
        _logger = _fixture.ServiceProvider.GetRequiredService<ILogger<CommerceStressTests>>();
    }

    [Fact]
    public async Task HighVolumeOrderProcessing_Under10Seconds_ShouldMaintainPerformance()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        
        var orderCount = 1000;
        var productCount = 50;
        var products = new List<InventoryItem>();
        
        // Setup inventory for multiple products
        for (int i = 0; i < productCount; i++)
        {
            var product = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Sku = $"STRESS-PRODUCT-{i:D3}",
                QuantityOnHand = 10000, // High stock to avoid conflicts
                QuantityReserved = 0,
                QuantityAvailable = 10000,
                ReorderLevel = 100,
                MaxStockLevel = 20000,
                Location = "Stress Test Warehouse",
                LastUpdated = DateTime.UtcNow
            };
            products.Add(product);
        }
        
        context.InventoryItems.AddRange(products);
        
        // Setup discount rules
        var discountRules = new List<DiscountRule>
        {
            new DiscountRule
            {
                Id = Guid.NewGuid(),
                Name = "Stress Test Bulk Discount",
                Description = "5% off for orders over $100",
                DiscountType = DiscountType.Percentage,
                Value = 5,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                MinimumOrderAmount = 100,
                MaximumDiscountAmount = 50,
                UsageLimit = 10000,
                UsageCount = 0,
                ApplicableToAllProducts = true
            },
            new DiscountRule
            {
                Id = Guid.NewGuid(),
                Name = "Stress Test Premium Discount",
                Description = "10% off for orders over $500",
                DiscountType = DiscountType.Percentage,
                Value = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                MinimumOrderAmount = 500,
                MaximumDiscountAmount = 100,
                UsageLimit = 5000,
                UsageCount = 0,
                ApplicableToAllProducts = true
            }
        };
        
        context.DiscountRules.AddRange(discountRules);
        await context.SaveChangesAsync();
        
        var random = new Random(42); // Fixed seed for reproducibility
        var orders = new List<Order>();
        var successfulReservations = new ConcurrentBag<Guid>();
        var failedOperations = new ConcurrentBag<string>();
        
        // Generate orders
        for (int i = 0; i < orderCount; i++)
        {
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var itemCount = random.Next(1, 6); // 1-5 items per order
            var orderItems = new List<OrderItem>();
            decimal subTotal = 0;
            
            for (int j = 0; j < itemCount; j++)
            {
                var product = products[random.Next(productCount)];
                var quantity = random.Next(1, 4); // 1-3 quantity per item
                var unitPrice = 20 + (decimal)(random.NextDouble() * 80); // $20-$100
                var totalPrice = quantity * unitPrice;
                
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = product.ProductId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Sku = product.Sku
                });
                
                subTotal += totalPrice;
            }
            
            orders.Add(new Order
            {
                Id = orderId,
                OrderNumber = $"STRESS-ORDER-{i:D6}",
                CustomerId = customerId,
                Status = OrderStatus.Pending,
                SubTotal = subTotal,
                TotalAmount = subTotal,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow,
                Items = orderItems
            });
        }
        
        // Act
        _logger.LogInformation($"Starting high volume order processing stress test with {orderCount} orders");
        var stopwatch = Stopwatch.StartNew();
        
        // Process orders in parallel
        var processingTasks = orders.Select(async order =>
        {
            try
            {
                // Step 1: Reserve inventory for all items
                var reservationTasks = order.Items.Select(async item =>
                {
                    var reservationResult = await reservationService.ReserveInventoryAsync(
                        item.ProductId, item.Quantity, order.CustomerId, TimeSpan.FromHours(1));
                    
                    if (reservationResult.Success)
                    {
                        successfulReservations.Add(reservationResult.ReservationId);
                        return true;
                    }
                    return false;
                });
                
                var reservationResults = await Task.WhenAll(reservationTasks);
                
                if (!reservationResults.All(r => r))
                {
                    failedOperations.Add($"Reservation failed for order {order.OrderNumber}");
                    return false;
                }
                
                // Step 2: Apply pricing and discounts
                var pricingResult = await pricingService.ApplyDiscountsAsync(order);
                
                if (pricingResult == null)
                {
                    failedOperations.Add($"Pricing failed for order {order.OrderNumber}");
                    return false;
                }
                
                // Update order with final pricing
                order.TotalAmount = pricingResult.FinalAmount;
                order.Status = OrderStatus.Completed;
                
                return true;
            }
            catch (Exception ex)
            {
                failedOperations.Add($"Exception in order {order.OrderNumber}: {ex.Message}");
                return false;
            }
        });
        
        var results = await Task.WhenAll(processingTasks);
        stopwatch.Stop();
        
        // Assert
        var successfulOrders = results.Count(r => r);
        var failedOrders = results.Count(r => !r);
        var processingTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var ordersPerSecond = orderCount / processingTimeSeconds;
        
        _logger.LogInformation($"Stress test completed in {processingTimeSeconds:F2} seconds");
        _logger.LogInformation($"Successful orders: {successfulOrders}/{orderCount}");
        _logger.LogInformation($"Failed orders: {failedOrders}");
        _logger.LogInformation($"Processing rate: {ordersPerSecond:F2} orders/second");
        _logger.LogInformation($"Successful reservations: {successfulReservations.Count}");
        
        // Performance assertions
        processingTimeSeconds.Should().BeLessThan(10, "Processing should complete within 10 seconds");
        ordersPerSecond.Should().BeGreaterThan(50, "Should process at least 50 orders per second");
        
        // Success rate assertions
        var successRate = (double)successfulOrders / orderCount * 100;
        successRate.Should().BeGreaterThan(95, "Success rate should be above 95%");
        
        // Log any failures for debugging
        if (failedOperations.Any())
        {
            _logger.LogWarning($"Failed operations: {string.Join(", ", failedOperations.Take(10))}");
        }
        
        // Verify database consistency
        var finalInventoryState = await context.InventoryItems
            .Where(i => products.Select(p => p.Id).Contains(i.Id))
            .ToListAsync();
        
        foreach (var inventory in finalInventoryState)
        {
            // Ensure no negative quantities
            inventory.QuantityOnHand.Should().BeGreaterOrEqualTo(0);
            inventory.QuantityAvailable.Should().BeGreaterOrEqualTo(0);
            inventory.QuantityReserved.Should().BeGreaterOrEqualTo(0);
            
            // Ensure inventory equation holds
            (inventory.QuantityOnHand - inventory.QuantityReserved)
                .Should().Be(inventory.QuantityAvailable);
        }
    }

    [Fact]
    public async Task MassiveSubscriptionRenewal_ShouldHandleThousandsOfRenewals()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var paymentProvider = _fixture.PaymentProviderMock;
        
        var subscriptionCount = 2000;
        var planCount = 10;
        var plans = new List<SubscriptionPlan>();
        var customers = new List<Customer>();
        var subscriptions = new List<Subscription>();
        
        // Setup subscription plans
        for (int i = 0; i < planCount; i++)
        {
            plans.Add(new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = $"Stress Plan {i + 1}",
                Description = $"Plan for stress testing {i + 1}",
                Price = 10 + (i * 5), // $10, $15, $20, etc.
                Currency = "USD",
                BillingCycle = BillingCycle.Monthly,
                IsActive = true,
                TrialPeriodDays = 0
            });
        }
        
        // Setup customers and subscriptions
        var random = new Random(42);
        for (int i = 0; i < subscriptionCount; i++)
        {
            var customerId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = customerId,
                Email = $"stress.test.{i}@example.com",
                FirstName = $"Stress{i}",
                LastName = "Test"
            };
            
            var plan = plans[random.Next(planCount)];
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-30),
                NextBillingDate = DateTime.UtcNow.Date, // Due for renewal
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-30),
                CurrentPeriodEnd = DateTime.UtcNow.Date,
                Customer = customer,
                Plan = plan
            };
            
            customers.Add(customer);
            subscriptions.Add(subscription);
        }
        
        context.SubscriptionPlans.AddRange(plans);
        context.Customers.AddRange(customers);
        context.Subscriptions.AddRange(subscriptions);
        await context.SaveChangesAsync();
        
        // Setup payment provider mock
        paymentProvider.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync((PaymentRequest request) => new PaymentResponse
            {
                Success = true,
                TransactionId = $"stripe_pi_stress_{Guid.NewGuid():N}[..16]",
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "succeeded",
                ProcessedAt = DateTime.UtcNow
            });
        
        var successfulRenewals = new ConcurrentBag<Guid>();
        var failedRenewals = new ConcurrentBag<string>();
        
        // Act
        _logger.LogInformation($"Starting massive subscription renewal stress test with {subscriptionCount} subscriptions");
        var stopwatch = Stopwatch.StartNew();
        
        // Process renewals in parallel batches
        var batchSize = 100;
        var batches = subscriptions
            .Select((s, i) => new { Subscription = s, Index = i })
            .GroupBy(x => x.Index / batchSize)
            .Select(g => g.Select(x => x.Subscription).ToList())
            .ToList();
        
        var batchTasks = batches.Select(async batch =>
        {
            var batchResults = await Task.WhenAll(batch.Select(async subscription =>
            {
                try
                {
                    var result = await subscriptionService.RenewSubscriptionAsync(subscription.Id);
                    if (result)
                    {
                        successfulRenewals.Add(subscription.Id);
                        return true;
                    }
                    else
                    {
                        failedRenewals.Add($"Renewal failed for subscription {subscription.Id}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    failedRenewals.Add($"Exception for subscription {subscription.Id}: {ex.Message}");
                    return false;
                }
            }));
            
            return batchResults;
        });
        
        var allResults = await Task.WhenAll(batchTasks);
        stopwatch.Stop();
        
        // Assert
        var totalSuccessful = allResults.SelectMany(r => r).Count(r => r);
        var totalFailed = subscriptionCount - totalSuccessful;
        var processingTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var renewalsPerSecond = subscriptionCount / processingTimeSeconds;
        
        _logger.LogInformation($"Massive renewal test completed in {processingTimeSeconds:F2} seconds");
        _logger.LogInformation($"Successful renewals: {totalSuccessful}/{subscriptionCount}");
        _logger.LogInformation($"Failed renewals: {totalFailed}");
        _logger.LogInformation($"Processing rate: {renewalsPerSecond:F2} renewals/second");
        
        // Performance assertions
        processingTimeSeconds.Should().BeLessThan(30, "Processing should complete within 30 seconds");
        renewalsPerSecond.Should().BeGreaterThan(50, "Should process at least 50 renewals per second");
        
        // Success rate assertions
        var successRate = (double)totalSuccessful / subscriptionCount * 100;
        successRate.Should().BeGreaterThan(98, "Success rate should be above 98%");
        
        // Verify database state
        var renewedSubscriptions = await context.Subscriptions
            .Where(s => subscriptions.Select(sub => sub.Id).Contains(s.Id))
            .ToListAsync();
        
        var activeRenewedCount = renewedSubscriptions.Count(s => s.Status == SubscriptionStatus.Active);
        activeRenewedCount.Should().Be(totalSuccessful, "All successful renewals should have active status");
        
        // Verify invoices were created
        var invoiceCount = await context.Invoices
            .CountAsync(i => subscriptions.Select(s => s.Id).Contains(i.SubscriptionId));
        
        invoiceCount.Should().Be(totalSuccessful, "Each successful renewal should have an invoice");
        
        // Verify payments were processed
        var paymentCount = await context.Payments
            .CountAsync(p => p.Status == PaymentStatus.Completed);
        
        paymentCount.Should().BeGreaterOrEqualTo(totalSuccessful, "Each successful renewal should have a payment");
    }

    [Fact]
    public async Task ExtremeConcurrentInventoryAccess_ShouldMaintainDataIntegrity()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var productId = Guid.NewGuid();
        var initialQuantity = 1000;
        var concurrentOperations = 500;
        var operationsPerUser = 5;
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "EXTREME-CONCURRENT-PRODUCT",
            QuantityOnHand = initialQuantity,
            QuantityReserved = 0,
            QuantityAvailable = initialQuantity,
            ReorderLevel = 50,
            MaxStockLevel = 2000,
            Location = "Extreme Test Warehouse",
            LastUpdated = DateTime.UtcNow
        };
        
        context.InventoryItems.Add(inventoryItem);
        await context.SaveChangesAsync();
        
        var successfulOperations = new ConcurrentBag<string>();
        var failedOperations = new ConcurrentBag<string>();
        var random = new Random();
        
        // Act
        _logger.LogInformation($"Starting extreme concurrent inventory access test with {concurrentOperations} concurrent users");
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, concurrentOperations).Select(async userId =>
        {
            using var userScope = _fixture.ServiceProvider.CreateScope();
            var reservationService = userScope.ServiceProvider.GetRequiredService<IReservationService>();
            var customerId = Guid.NewGuid();
            
            var userOperations = new List<Task<bool>>();
            
            for (int op = 0; op < operationsPerUser; op++)
            {
                userOperations.Add(Task.Run(async () =>
                {
                    try
                    {
                        var quantity = random.Next(1, 4); // 1-3 items
                        var operationType = random.Next(0, 3); // 0=reserve, 1=confirm, 2=cancel
                        
                        switch (operationType)
                        {
                            case 0: // Reserve
                                var reserveResult = await reservationService.ReserveInventoryAsync(
                                    productId, quantity, customerId, TimeSpan.FromMinutes(30));
                                
                                if (reserveResult.Success)
                                {
                                    successfulOperations.Add($"User{userId}-Op{op}: Reserved {quantity} items");
                                    
                                    // Randomly confirm or cancel the reservation
                                    await Task.Delay(random.Next(10, 100)); // Simulate processing time
                                    
                                    if (random.Next(0, 2) == 0)
                                    {
                                        var confirmResult = await reservationService.ConfirmReservationAsync(reserveResult.ReservationId);
                                        if (confirmResult)
                                        {
                                            successfulOperations.Add($"User{userId}-Op{op}: Confirmed reservation");
                                        }
                                    }
                                    else
                                    {
                                        var cancelResult = await reservationService.CancelReservationAsync(reserveResult.ReservationId);
                                        if (cancelResult)
                                        {
                                            successfulOperations.Add($"User{userId}-Op{op}: Cancelled reservation");
                                        }
                                    }
                                    
                                    return true;
                                }
                                else
                                {
                                    failedOperations.Add($"User{userId}-Op{op}: Failed to reserve {quantity} items");
                                    return false;
                                }
                                
                            default:
                                return true; // Skip other operation types for simplicity
                        }
                    }
                    catch (Exception ex)
                    {
                        failedOperations.Add($"User{userId}-Op{op}: Exception - {ex.Message}");
                        return false;
                    }
                }));
            }
            
            var results = await Task.WhenAll(userOperations);
            return results.Count(r => r);
        });
        
        var userResults = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var totalSuccessfulOps = successfulOperations.Count;
        var totalFailedOps = failedOperations.Count;
        var totalOperations = concurrentOperations * operationsPerUser;
        var processingTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var operationsPerSecond = totalOperations / processingTimeSeconds;
        
        _logger.LogInformation($"Extreme concurrent test completed in {processingTimeSeconds:F2} seconds");
        _logger.LogInformation($"Total operations: {totalOperations}");
        _logger.LogInformation($"Successful operations: {totalSuccessfulOps}");
        _logger.LogInformation($"Failed operations: {totalFailedOps}");
        _logger.LogInformation($"Processing rate: {operationsPerSecond:F2} operations/second");
        
        // Performance assertions
        processingTimeSeconds.Should().BeLessThan(60, "Processing should complete within 60 seconds");
        operationsPerSecond.Should().BeGreaterThan(20, "Should process at least 20 operations per second");
        
        // Verify final inventory integrity
        var finalInventory = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        
        finalInventory.Should().NotBeNull();
        
        // Ensure no negative quantities
        finalInventory!.QuantityOnHand.Should().BeGreaterOrEqualTo(0);
        finalInventory.QuantityAvailable.Should().BeGreaterOrEqualTo(0);
        finalInventory.QuantityReserved.Should().BeGreaterOrEqualTo(0);
        
        // Ensure inventory equation holds
        (finalInventory.QuantityOnHand - finalInventory.QuantityReserved)
            .Should().Be(finalInventory.QuantityAvailable);
        
        // Verify total quantity doesn't exceed initial quantity
        (finalInventory.QuantityOnHand + finalInventory.QuantityReserved)
            .Should().BeLessOrEqualTo(initialQuantity);
        
        // Verify ledger entries are consistent
        var ledgerEntries = await context.InventoryLedgers
            .Where(l => l.InventoryItemId == inventoryItem.Id)
            .ToListAsync();
        
        var totalLedgerQuantityChange = ledgerEntries.Sum(l => l.Quantity);
        var expectedQuantityChange = finalInventory.QuantityOnHand - initialQuantity;
        
        totalLedgerQuantityChange.Should().Be(expectedQuantityChange, 
            "Ledger entries should match actual inventory changes");
        
        _logger.LogInformation($"Final inventory state: OnHand={finalInventory.QuantityOnHand}, Reserved={finalInventory.QuantityReserved}, Available={finalInventory.QuantityAvailable}");
        _logger.LogInformation($"Ledger entries: {ledgerEntries.Count}, Total quantity change: {totalLedgerQuantityChange}");
    }

    [Fact]
    public async Task LargeScaleReconciliation_ShouldProcessThousandsOfTransactions()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        
        var transactionCount = 5000;
        var matchRate = 0.95; // 95% of transactions should match
        var orders = new List<Order>();
        var payments = new List<Payment>();
        var transactions = new List<StatementTransaction>();
        
        var random = new Random(42);
        
        // Generate matched transactions
        var matchedCount = (int)(transactionCount * matchRate);
        for (int i = 0; i < matchedCount; i++)
        {
            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var externalTxnId = $"stripe_pi_large_scale_{i:D6}";
            var amount = 10 + (decimal)(random.NextDouble() * 490); // $10-$500
            
            var order = new Order
            {
                Id = orderId,
                OrderNumber = $"LARGE-SCALE-{i:D6}",
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Completed,
                TotalAmount = amount,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 48))
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
                CreatedAt = order.CreatedAt.AddMinutes(random.Next(1, 30)),
                Order = order
            };
            
            var transaction = new StatementTransaction
            {
                Id = $"large_scale_txn_{i:D6}",
                ExternalTransactionId = externalTxnId,
                Amount = amount,
                Currency = "USD",
                TransactionDate = payment.CreatedAt.AddMinutes(random.Next(-5, 15)),
                ReferenceNumber = order.OrderNumber,
                TransactionType = "Payment",
                Status = "Completed"
            };
            
            orders.Add(order);
            payments.Add(payment);
            transactions.Add(transaction);
        }
        
        // Generate unmatched transactions
        var unmatchedCount = transactionCount - matchedCount;
        for (int i = 0; i < unmatchedCount; i++)
        {
            var transaction = new StatementTransaction
            {
                Id = $"large_scale_unmatched_{i:D6}",
                ExternalTransactionId = $"stripe_pi_unmatched_{i:D6}",
                Amount = 10 + (decimal)(random.NextDouble() * 200),
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddHours(-random.Next(1, 48)),
                ReferenceNumber = $"UNMATCHED-{i:D6}",
                TransactionType = "Payment",
                Status = "Completed"
            };
            
            transactions.Add(transaction);
        }
        
        // Shuffle transactions to simulate real-world randomness
        transactions = transactions.OrderBy(t => random.Next()).ToList();
        
        context.Orders.AddRange(orders);
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        
        var statement = new PaymentGatewayStatement
        {
            Id = "large_scale_stmt_001",
            PaymentProvider = "Stripe",
            PeriodStart = DateTime.UtcNow.AddDays(-2),
            PeriodEnd = DateTime.UtcNow,
            Transactions = transactions
        };
        
        // Act
        _logger.LogInformation($"Starting large scale reconciliation test with {transactionCount} transactions");
        var stopwatch = Stopwatch.StartNew();
        
        var reconciliationResult = await reconciliationService.ProcessStatementAsync(statement);
        
        stopwatch.Stop();
        
        // Assert
        var processingTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        var transactionsPerSecond = transactionCount / processingTimeSeconds;
        
        _logger.LogInformation($"Large scale reconciliation completed in {processingTimeSeconds:F2} seconds");
        _logger.LogInformation($"Processing rate: {transactionsPerSecond:F2} transactions/second");
        _logger.LogInformation($"Match rate: {reconciliationResult.Summary.MatchRate:F2}%");
        
        // Performance assertions
        processingTimeSeconds.Should().BeLessThan(30, "Processing should complete within 30 seconds");
        transactionsPerSecond.Should().BeGreaterThan(100, "Should process at least 100 transactions per second");
        
        // Accuracy assertions
        reconciliationResult.Success.Should().BeTrue();
        reconciliationResult.Summary.TotalTransactions.Should().Be(transactionCount);
        reconciliationResult.Summary.MatchedTransactions.Should().Be(matchedCount);
        reconciliationResult.Summary.UnmatchedTransactions.Should().Be(unmatchedCount);
        reconciliationResult.Summary.MatchRate.Should().BeApproximately(95.0m, 0.1m);
        
        // Verify database state
        var session = await context.ReconciliationSessions
            .Include(s => s.MatchedTransactions)
            .Include(s => s.UnmatchedTransactions)
            .FirstOrDefaultAsync(s => s.StatementId == statement.Id);
        
        session.Should().NotBeNull();
        session!.Status.Should().Be(ReconciliationStatus.Completed);
        session.MatchedTransactions.Should().HaveCount(matchedCount);
        session.UnmatchedTransactions.Should().HaveCount(unmatchedCount);
        
        // Verify confidence scores
        var highConfidenceMatches = session.MatchedTransactions
            .Count(m => m.ConfidenceScore >= 95);
        
        highConfidenceMatches.Should().BeGreaterThan(matchedCount * 0.8, 
            "At least 80% of matches should have high confidence scores");
    }
}