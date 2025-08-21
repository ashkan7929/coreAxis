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
using Moq;

namespace CoreAxis.Modules.CommerceModule.Tests.Regression
{
    /// <summary>
    /// Regression tests for Commerce Module
    /// Tests to prevent previously fixed bugs from reoccurring
    /// </summary>
    [Trait(TestTraits.Category, TestCategories.Regression)]
    public class CommerceRegressionTests : CommerceTestBase
    {
        #region Order Processing Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        [Trait(TestTraits.BugId, "BUG-001")]
        public async Task OrderCancellation_ShouldReleaseInventoryReservation_RegressionTest()
        {
            // Regression test for bug where cancelled orders didn't release inventory
            // Bug ID: BUG-001
            // Fixed in: v1.2.0
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 50,
                    UnitPrice = new Money(25.00m, "USD")
                }
            };

            var order = await OrderService.CreateOrderAsync(
                customer.Id, 
                orderItems, 
                customer.DefaultShippingAddress);

            // Verify inventory is reserved
            var inventoryAfterOrder = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            inventoryAfterOrder.QuantityReserved.Should().Be(50);
            inventoryAfterOrder.QuantityAvailable.Should().Be(50);

            // Act - Cancel the order
            await OrderService.CancelOrderAsync(order.Id, "Customer requested cancellation");

            // Assert - Inventory should be released
            var inventoryAfterCancellation = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            inventoryAfterCancellation.QuantityReserved.Should().Be(0);
            inventoryAfterCancellation.QuantityAvailable.Should().Be(100);
            
            var cancelledOrder = await Context.Orders.FindAsync(order.Id);
            cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        [Trait(TestTraits.BugId, "BUG-002")]
        public async Task OrderTotalCalculation_ShouldIncludeTaxAndShipping_RegressionTest()
        {
            // Regression test for bug where order total didn't include tax and shipping
            // Bug ID: BUG-002
            // Fixed in: v1.1.5
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 2,
                    UnitPrice = new Money(50.00m, "USD")
                }
            };

            // Act
            var order = await OrderService.CreateOrderAsync(
                customer.Id, 
                orderItems, 
                customer.DefaultShippingAddress);

            // Assert
            var subtotal = new Money(100.00m, "USD"); // 2 * $50
            var expectedTax = new Money(8.00m, "USD"); // 8% tax
            var expectedShipping = new Money(10.00m, "USD"); // Standard shipping
            var expectedTotal = new Money(118.00m, "USD"); // $100 + $8 + $10

            order.SubtotalAmount.Should().Be(subtotal);
            order.TaxAmount.Should().Be(expectedTax);
            order.ShippingAmount.Should().Be(expectedShipping);
            order.TotalAmount.Should().Be(expectedTotal);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-003")]
        public async Task ConcurrentOrderCreation_ShouldNotOversellInventory_RegressionTest()
        {
            // Regression test for race condition in inventory reservation
            // Bug ID: BUG-003
            // Fixed in: v1.3.0
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 10);
            
            var concurrentOrders = 20; // More orders than available inventory
            var successfulOrders = 0;
            var failedOrders = 0;
            var lockObject = new object();

            // Act - Create concurrent orders
            var orderTasks = Enumerable.Range(0, concurrentOrders).Select(async i =>
            {
                try
                {
                    var orderItems = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            InventoryItemId = inventoryItem.Id,
                            Quantity = 1,
                            UnitPrice = new Money(20.00m, "USD")
                        }
                    };

                    var order = await OrderService.CreateOrderAsync(
                        customer.Id, 
                        orderItems, 
                        customer.DefaultShippingAddress);
                    
                    lock (lockObject)
                    {
                        successfulOrders++;
                    }
                    return true;
                }
                catch
                {
                    lock (lockObject)
                    {
                        failedOrders++;
                    }
                    return false;
                }
            });

            await Task.WhenAll(orderTasks);

            // Assert
            (successfulOrders + failedOrders).Should().Be(concurrentOrders);
            successfulOrders.Should().BeLessOrEqualTo(10); // Should not oversell
            
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityReserved.Should().Be(successfulOrders);
            finalInventory.QuantityAvailable.Should().Be(10 - successfulOrders);
        }

        #endregion

        #region Payment Processing Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        [Trait(TestTraits.BugId, "BUG-004")]
        public async Task FailedPayment_ShouldNotMarkOrderAsProcessed_RegressionTest()
        {
            // Regression test for bug where failed payments still marked orders as processed
            // Bug ID: BUG-004
            // Fixed in: v1.2.3
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m, "USD"));
            
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateFailedPaymentResponse("Insufficient funds"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PaymentProcessingException>(
                () => PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard));
            
            exception.Message.Should().Contain("Insufficient funds");
            
            // Verify order status remains unchanged
            var orderAfterFailedPayment = await Context.Orders.FindAsync(order.Id);
            orderAfterFailedPayment.Status.Should().Be(OrderStatus.Pending);
            orderAfterFailedPayment.PaymentStatus.Should().Be(PaymentStatus.Failed);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-005")]
        public async Task DuplicatePaymentProcessing_ShouldBeIdempotent_RegressionTest()
        {
            // Regression test for bug where duplicate payment processing caused double charges
            // Bug ID: BUG-005
            // Fixed in: v1.4.0
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m, "USD"));
            
            var paymentCallCount = 0;
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(() =>
                {
                    Interlocked.Increment(ref paymentCallCount);
                    return CommerceTestUtilities.CreateSuccessfulPaymentResponse();
                });

            // Act - Process payment twice (simulating duplicate request)
            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            
            // Second call should be idempotent
            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);

            // Assert
            paymentCallCount.Should().Be(1); // Payment provider should only be called once
            
            var processedOrder = await Context.Orders.FindAsync(order.Id);
            processedOrder.Status.Should().Be(OrderStatus.Processing);
            processedOrder.PaymentStatus.Should().Be(PaymentStatus.Completed);
            
            // Verify only one payment record exists
            var payments = await Context.Payments.Where(p => p.OrderId == order.Id).ToListAsync();
            payments.Should().HaveCount(1);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-006")]
        public async Task PartialRefund_ShouldNotExceedOriginalAmount_RegressionTest()
        {
            // Regression test for bug where partial refunds could exceed original payment
            // Bug ID: BUG-006
            // Fixed in: v1.3.2
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m, "USD"));
            
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());
            
            MockPaymentService.Setup(x => x.ProcessRefundAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulRefundResponse());

            await OrderService.ProcessOrderAsync(order.Id);
            await OrderService.CompleteOrderAsync(order.Id);

            // Create first partial refund
            var firstRefund = await RefundService.CreateRefundRequestAsync(
                order.Id, 
                new Money(60.00m, "USD"), 
                "Partial refund 1");
            await RefundService.ProcessRefundAsync(firstRefund.Id);

            // Act & Assert - Second refund should fail if it would exceed original amount
            var exception = await Assert.ThrowsAsync<InvalidRefundAmountException>(
                () => RefundService.CreateRefundRequestAsync(
                    order.Id, 
                    new Money(50.00m, "USD"), // $60 + $50 = $110 > $100 original
                    "Partial refund 2"));
            
            exception.Message.Should().Contain("exceeds remaining refundable amount");
            
            // Verify total refunded amount
            var refunds = await Context.RefundRequests.Where(r => r.OrderId == order.Id).ToListAsync();
            var totalRefunded = refunds.Where(r => r.Status == RefundStatus.Completed)
                                     .Sum(r => r.Amount.Amount);
            totalRefunded.Should().Be(60.00m);
        }

        #endregion

        #region Inventory Management Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        [Trait(TestTraits.BugId, "BUG-007")]
        public async Task InventoryReservationExpiry_ShouldReleaseReservedStock_RegressionTest()
        {
            // Regression test for bug where expired reservations weren't automatically released
            // Bug ID: BUG-007
            // Fixed in: v1.5.0
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            // Create reservation with short expiry
            var reservationId = await InventoryService.ReserveAsync(
                inventoryItem.Id, 
                50, 
                "Test reservation",
                expiresAt: DateTime.UtcNow.AddSeconds(1));

            // Verify reservation is active
            var inventoryAfterReservation = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            inventoryAfterReservation.QuantityReserved.Should().Be(50);
            inventoryAfterReservation.QuantityAvailable.Should().Be(50);

            // Act - Wait for reservation to expire
            await Task.Delay(2000); // Wait 2 seconds
            
            // Trigger cleanup process
            await InventoryService.CleanupExpiredReservationsAsync();

            // Assert - Reservation should be released
            var inventoryAfterCleanup = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            inventoryAfterCleanup.QuantityReserved.Should().Be(0);
            inventoryAfterCleanup.QuantityAvailable.Should().Be(100);
            
            var expiredReservation = await Context.InventoryReservations.FindAsync(reservationId);
            expiredReservation.Status.Should().Be(ReservationStatus.Expired);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-008")]
        public async Task NegativeInventoryUpdate_ShouldBeRejected_RegressionTest()
        {
            // Regression test for bug where negative inventory updates were allowed
            // Bug ID: BUG-008
            // Fixed in: v1.2.1
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 10);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidInventoryOperationException>(
                () => InventoryService.UpdateStockAsync(
                    inventoryItem.Id, 
                    -5, // Negative quantity
                    "Invalid negative update"));
            
            exception.Message.Should().Contain("cannot be negative");
            
            // Verify inventory remains unchanged
            var unchangedInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            unchangedInventory.QuantityOnHand.Should().Be(10);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-009")]
        public async Task InventoryLedger_ShouldMaintainAccurateHistory_RegressionTest()
        {
            // Regression test for bug where inventory ledger entries were inconsistent
            // Bug ID: BUG-009
            // Fixed in: v1.4.2
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);

            // Act - Perform various inventory operations
            await InventoryService.UpdateStockAsync(inventoryItem.Id, 120, "Stock increase");
            await InventoryService.ReserveAsync(inventoryItem.Id, 30, "Test reservation");
            await InventoryService.UpdateStockAsync(inventoryItem.Id, 110, "Stock adjustment");
            await InventoryService.ReleaseReservationAsync(inventoryItem.Id, 10, "Partial release");

            // Assert - Verify ledger entries
            var ledgerEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            ledgerEntries.Should().HaveCount(5); // Initial + 4 operations
            
            // Verify entry types and quantities
            ledgerEntries[0].EntryType.Should().Be(InventoryLedgerEntryType.Initial);
            ledgerEntries[0].Quantity.Should().Be(100);
            
            ledgerEntries[1].EntryType.Should().Be(InventoryLedgerEntryType.StockIncrease);
            ledgerEntries[1].Quantity.Should().Be(20);
            
            ledgerEntries[2].EntryType.Should().Be(InventoryLedgerEntryType.Reservation);
            ledgerEntries[2].Quantity.Should().Be(-30);
            
            ledgerEntries[3].EntryType.Should().Be(InventoryLedgerEntryType.StockAdjustment);
            ledgerEntries[3].Quantity.Should().Be(-10);
            
            ledgerEntries[4].EntryType.Should().Be(InventoryLedgerEntryType.ReservationRelease);
            ledgerEntries[4].Quantity.Should().Be(10);
            
            // Verify final inventory state
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityOnHand.Should().Be(110);
            finalInventory.QuantityReserved.Should().Be(20); // 30 - 10
            finalInventory.QuantityAvailable.Should().Be(90); // 110 - 20
        }

        #endregion

        #region Discount and Pricing Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Discounts)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-010")]
        public async Task ExpiredCoupon_ShouldNotApplyDiscount_RegressionTest()
        {
            // Regression test for bug where expired coupons were still being applied
            // Bug ID: BUG-010
            // Fixed in: v1.3.1
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            var expiredCoupon = new Coupon
            {
                Code = "EXPIRED10",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
                IsActive = true,
                UsageLimit = 100,
                UsedCount = 0
            };
            
            Context.Coupons.Add(expiredCoupon);
            await Context.SaveChangesAsync();
            
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 1,
                    UnitPrice = new Money(100.00m, "USD")
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidCouponException>(
                () => OrderService.CreateOrderAsync(
                    customer.Id, 
                    orderItems, 
                    customer.DefaultShippingAddress,
                    couponCode: "EXPIRED10"));
            
            exception.Message.Should().Contain("expired");
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Discounts)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-011")]
        public async Task CouponUsageLimit_ShouldBeEnforced_RegressionTest()
        {
            // Regression test for bug where coupon usage limits weren't properly enforced
            // Bug ID: BUG-011
            // Fixed in: v1.4.1
            
            // Arrange
            var customers = new List<Customer>();
            for (int i = 0; i < 3; i++)
            {
                customers.Add(await CreateTestCustomerAsync());
            }
            
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            var limitedCoupon = new Coupon
            {
                Code = "LIMITED2",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 20,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                UsageLimit = 2, // Only 2 uses allowed
                UsedCount = 0
            };
            
            Context.Coupons.Add(limitedCoupon);
            await Context.SaveChangesAsync();

            // Act - Use coupon twice (should succeed)
            for (int i = 0; i < 2; i++)
            {
                var orderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        InventoryItemId = inventoryItem.Id,
                        Quantity = 1,
                        UnitPrice = new Money(100.00m, "USD")
                    }
                };

                var order = await OrderService.CreateOrderAsync(
                    customers[i].Id, 
                    orderItems, 
                    customers[i].DefaultShippingAddress,
                    couponCode: "LIMITED2");
                
                order.DiscountAmount.Amount.Should().Be(20.00m);
            }

            // Third use should fail
            var thirdOrderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 1,
                    UnitPrice = new Money(100.00m, "USD")
                }
            };

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidCouponException>(
                () => OrderService.CreateOrderAsync(
                    customers[2].Id, 
                    thirdOrderItems, 
                    customers[2].DefaultShippingAddress,
                    couponCode: "LIMITED2"));
            
            exception.Message.Should().Contain("usage limit");
            
            // Verify coupon usage count
            var updatedCoupon = await Context.Coupons.FindAsync(limitedCoupon.Id);
            updatedCoupon.UsedCount.Should().Be(2);
        }

        #endregion

        #region Subscription Management Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        [Trait(TestTraits.BugId, "BUG-012")]
        public async Task SubscriptionRenewal_ShouldHandlePaymentFailure_RegressionTest()
        {
            // Regression test for bug where failed subscription renewals weren't properly handled
            // Bug ID: BUG-012
            // Fixed in: v1.5.1
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var subscriptionPlan = await CreateTestSubscriptionPlanAsync();
            
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id, 
                subscriptionPlan.Id);
            
            // Setup payment failure for renewal
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateFailedPaymentResponse("Card declined"));

            // Act - Attempt renewal
            var renewalResult = await SubscriptionService.ProcessRenewalAsync(subscription.Id);

            // Assert
            renewalResult.Should().BeFalse();
            
            var updatedSubscription = await Context.Subscriptions.FindAsync(subscription.Id);
            updatedSubscription.Status.Should().Be(SubscriptionStatus.PaymentFailed);
            updatedSubscription.NextBillingDate.Should().BeAfter(DateTime.UtcNow); // Should be rescheduled
            
            // Verify retry attempt is scheduled
            var retryAttempts = await Context.SubscriptionRetryAttempts
                .Where(r => r.SubscriptionId == subscription.Id)
                .ToListAsync();
            retryAttempts.Should().HaveCount(1);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-013")]
        public async Task SubscriptionUpgrade_ShouldCalculateProration_RegressionTest()
        {
            // Regression test for bug where subscription upgrades didn't calculate proration correctly
            // Bug ID: BUG-013
            // Fixed in: v1.4.3
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var basicPlan = await CreateTestSubscriptionPlanAsync(
                name: "Basic", 
                price: new Money(10.00m, "USD"), 
                billingCycle: BillingCycle.Monthly);
            
            var premiumPlan = await CreateTestSubscriptionPlanAsync(
                name: "Premium", 
                price: new Money(30.00m, "USD"), 
                billingCycle: BillingCycle.Monthly);

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id, 
                basicPlan.Id);
            
            // Simulate 15 days into the billing cycle
            var upgradeDate = subscription.CurrentPeriodStart.AddDays(15);
            
            // Act
            var upgradeResult = await SubscriptionService.UpgradeSubscriptionAsync(
                subscription.Id, 
                premiumPlan.Id, 
                upgradeDate);

            // Assert
            upgradeResult.Should().NotBeNull();
            
            // Verify proration calculation
            // Remaining days: 15 (out of 30)
            // Basic plan unused: $5.00 (15/30 * $10)
            // Premium plan prorated: $15.00 (15/30 * $30)
            // Net charge: $10.00 ($15 - $5)
            
            var prorationInvoice = await Context.Invoices
                .Where(i => i.SubscriptionId == subscription.Id && i.Type == InvoiceType.Proration)
                .FirstOrDefaultAsync();
            
            prorationInvoice.Should().NotBeNull();
            prorationInvoice.Amount.Amount.Should().Be(10.00m);
            
            var updatedSubscription = await Context.Subscriptions.FindAsync(subscription.Id);
            updatedSubscription.PlanId.Should().Be(premiumPlan.Id);
        }

        #endregion

        #region Data Consistency Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.DataConsistency)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        [Trait(TestTraits.BugId, "BUG-014")]
        public async Task TransactionRollback_ShouldMaintainDataIntegrity_RegressionTest()
        {
            // Regression test for bug where failed transactions didn't properly rollback
            // Bug ID: BUG-014
            // Fixed in: v1.6.0
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            var initialInventoryState = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            var initialInventoryQuantity = initialInventoryState.QuantityOnHand;
            var initialReservedQuantity = initialInventoryState.QuantityReserved;

            // Setup payment service to fail
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ThrowsAsync(new PaymentProviderException("Payment provider unavailable"));

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 50,
                    UnitPrice = new Money(25.00m, "USD")
                }
            };

            var order = await OrderService.CreateOrderAsync(
                customer.Id, 
                orderItems, 
                customer.DefaultShippingAddress);

            // Act & Assert - Payment processing should fail and rollback
            var exception = await Assert.ThrowsAsync<PaymentProcessingException>(
                () => PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard));
            
            exception.InnerException.Should().BeOfType<PaymentProviderException>();
            
            // Verify data integrity after rollback
            var orderAfterFailure = await Context.Orders.FindAsync(order.Id);
            orderAfterFailure.Status.Should().Be(OrderStatus.Pending);
            orderAfterFailure.PaymentStatus.Should().Be(PaymentStatus.Failed);
            
            var inventoryAfterFailure = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            inventoryAfterFailure.QuantityOnHand.Should().Be(initialInventoryQuantity);
            inventoryAfterFailure.QuantityReserved.Should().Be(initialReservedQuantity + 50); // Reservation should remain
            
            // Verify no payment records were created
            var payments = await Context.Payments.Where(p => p.OrderId == order.Id).ToListAsync();
            payments.Should().BeEmpty();
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.DataConsistency)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-015")]
        public async Task ConcurrentDataModification_ShouldHandleOptimisticLocking_RegressionTest()
        {
            // Regression test for bug where concurrent modifications caused data corruption
            // Bug ID: BUG-015
            // Fixed in: v1.5.2
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            // Simulate concurrent modifications
            var concurrentTasks = new List<Task<bool>>();
            
            for (int i = 0; i < 10; i++)
            {
                var taskIndex = i;
                concurrentTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Each task tries to update inventory
                        using var scope = ServiceProvider.CreateScope();
                        var scopedInventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                        
                        var currentItem = await Context.InventoryItems.FindAsync(inventoryItem.Id);
                        var newQuantity = currentItem.QuantityOnHand + taskIndex;
                        
                        await scopedInventoryService.UpdateStockAsync(
                            inventoryItem.Id, 
                            newQuantity, 
                            $"Concurrent update {taskIndex}");
                        
                        return true;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Expected for some tasks due to optimistic locking
                        return false;
                    }
                }));
            }

            // Act
            var results = await Task.WhenAll(concurrentTasks);

            // Assert
            var successfulUpdates = results.Count(r => r);
            successfulUpdates.Should().BeGreaterThan(0); // At least one should succeed
            successfulUpdates.Should().BeLessThan(10); // Not all should succeed due to concurrency
            
            // Verify final data integrity
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityOnHand.Should().BeGreaterThan(100); // Should be increased
            finalInventory.QuantityOnHand.Should().BeLessThan(145); // But not by all attempts
            
            // Verify ledger entries match the successful updates
            var ledgerEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id && e.EntryType == InventoryLedgerEntryType.StockIncrease)
                .ToListAsync();
            
            ledgerEntries.Should().HaveCount(successfulUpdates);
        }

        #endregion

        #region Performance Regression Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Performance)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        [Trait(TestTraits.BugId, "BUG-016")]
        public async Task LargeOrderProcessing_ShouldNotTimeOut_RegressionTest()
        {
            // Regression test for bug where large orders caused timeouts
            // Bug ID: BUG-016
            // Fixed in: v1.6.1
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItems = new List<InventoryItem>();
            
            // Create 100 inventory items
            for (int i = 0; i < 100; i++)
            {
                inventoryItems.Add(await CreateTestInventoryItemAsync(quantity: 1000));
            }

            // Create large order with 100 items
            var orderItems = inventoryItems.Select(item => new OrderItem
            {
                InventoryItemId = item.Id,
                Quantity = 5,
                UnitPrice = new Money(10.00m + (item.Id % 50), "USD")
            }).ToList();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var order = await OrderService.CreateOrderAsync(
                customer.Id, 
                orderItems, 
                customer.DefaultShippingAddress);

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
            
            order.Items.Should().HaveCount(100);
            order.SubtotalAmount.Amount.Should().BeGreaterThan(0);
            
            // Verify all inventory reservations were created
            var totalReserved = await Context.InventoryReservations
                .Where(r => r.OrderId == order.Id)
                .SumAsync(r => r.Quantity);
            
            totalReserved.Should().Be(500); // 100 items * 5 quantity each
        }

        #endregion
    }
}