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
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Extensions.Hosting;

namespace CoreAxis.Modules.CommerceModule.Tests.Integration
{
    /// <summary>
    /// Integration tests for Commerce Module
    /// Tests end-to-end scenarios and cross-service interactions
    /// </summary>
    [Trait(TestTraits.Category, TestCategories.Integration)]
    public class CommerceIntegrationTests : CommerceTestBase
    {
        #region Order Processing Integration Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task CompleteOrderWorkflow_ShouldProcessSuccessfully()
        {
            // Integration test for complete order processing workflow
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItems = new List<InventoryItem>();
            
            for (int i = 0; i < 3; i++)
            {
                inventoryItems.Add(await CreateTestInventoryItemAsync($"Product {i}", quantity: 100));
            }

            var orderItems = inventoryItems.Select(item => new OrderItem
            {
                InventoryItemId = item.Id,
                Quantity = 2,
                UnitPrice = new Money(25.00m, "USD")
            }).ToList();

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            MockShippingService.Setup(x => x.CreateShipmentAsync(It.IsAny<ShipmentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulShipmentResponse());

            MockNotificationService.Setup(x => x.SendOrderConfirmationAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act - Execute complete workflow
            
            // Step 1: Create order
            var order = await OrderService.CreateOrderAsync(
                customer.Id,
                orderItems,
                customer.DefaultShippingAddress);

            // Step 2: Process payment
            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);

            // Step 3: Process order (inventory allocation, shipping)
            await OrderService.ProcessOrderAsync(order.Id);

            // Step 4: Ship order
            await OrderService.ShipOrderAsync(order.Id);

            // Step 5: Complete order
            await OrderService.CompleteOrderAsync(order.Id);

            // Assert - Verify complete workflow
            var finalOrder = await Context.Orders
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            finalOrder.Should().NotBeNull();
            finalOrder.Status.Should().Be(OrderStatus.Completed);
            finalOrder.Items.Should().HaveCount(3);
            finalOrder.TotalAmount.Amount.Should().Be(150.00m); // 3 items * 2 qty * $25

            // Verify payment was processed
            var payment = finalOrder.Payments.FirstOrDefault();
            payment.Should().NotBeNull();
            payment.Status.Should().Be(PaymentStatus.Completed);
            payment.Amount.Amount.Should().Be(150.00m);

            // Verify inventory was updated
            var updatedInventoryItems = await Context.InventoryItems
                .Where(i => inventoryItems.Select(item => item.Id).Contains(i.Id))
                .ToListAsync();

            updatedInventoryItems.Should().AllSatisfy(item =>
                item.QuantityOnHand.Should().Be(98)); // 100 - 2

            // Verify inventory ledger entries
            var ledgerEntries = await Context.InventoryLedgerEntries
                .Where(e => inventoryItems.Select(item => item.Id).Contains(e.InventoryItemId))
                .Where(e => e.EntryType == InventoryLedgerEntryType.Sale)
                .ToListAsync();

            ledgerEntries.Should().HaveCount(3);
            ledgerEntries.Should().AllSatisfy(entry => entry.Quantity.Should().Be(-2));

            // Verify shipment was created
            var shipment = finalOrder.Shipments.FirstOrDefault();
            shipment.Should().NotBeNull();
            shipment.Status.Should().Be(ShipmentStatus.Delivered);

            // Verify notifications were sent
            MockNotificationService.Verify(
                x => x.SendOrderConfirmationAsync(order.Id),
                Times.AtLeastOnce);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task OrderWithInsufficientInventory_ShouldHandleGracefully()
        {
            // Integration test for order processing with insufficient inventory
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 5); // Limited stock

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 10, // More than available
                    UnitPrice = new Money(25.00m, "USD")
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InsufficientInventoryException>(
                () => OrderService.CreateOrderAsync(
                    customer.Id,
                    orderItems,
                    customer.DefaultShippingAddress));

            exception.Should().NotBeNull();
            exception.Message.Should().Contain("Insufficient inventory");

            // Verify inventory was not affected
            var unchangedInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            unchangedInventory.QuantityOnHand.Should().Be(5);
            unchangedInventory.QuantityReserved.Should().Be(0);

            // Verify no order was created
            var orders = await Context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .ToListAsync();

            orders.Should().BeEmpty();
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task OrderCancellation_ShouldReverseAllOperations()
        {
            // Integration test for order cancellation and operation reversal
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 5,
                    UnitPrice = new Money(30.00m, "USD")
                }
            };

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            MockPaymentService.Setup(x => x.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulRefundResponse());

            // Act - Create and process order, then cancel
            var order = await OrderService.CreateOrderAsync(
                customer.Id,
                orderItems,
                customer.DefaultShippingAddress);

            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            await OrderService.ProcessOrderAsync(order.Id);

            // Verify order is processed
            var processedOrder = await Context.Orders.FindAsync(order.Id);
            processedOrder.Status.Should().Be(OrderStatus.Processing);

            // Cancel the order
            await OrderService.CancelOrderAsync(order.Id, "Customer request");

            // Assert - Verify cancellation effects
            var cancelledOrder = await Context.Orders
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            cancelledOrder.Should().NotBeNull();
            cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
            cancelledOrder.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            cancelledOrder.CancellationReason.Should().Be("Customer request");

            // Verify inventory was restored
            var restoredInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            restoredInventory.QuantityOnHand.Should().Be(100); // Back to original
            restoredInventory.QuantityReserved.Should().Be(0);

            // Verify refund was processed
            var refundPayment = await Context.Payments
                .Where(p => p.OrderId == order.Id)
                .Where(p => p.Type == PaymentType.Refund)
                .FirstOrDefaultAsync();

            refundPayment.Should().NotBeNull();
            refundPayment.Amount.Amount.Should().Be(150.00m); // 5 * $30
            refundPayment.Status.Should().Be(PaymentStatus.Completed);

            // Verify inventory ledger entries for cancellation
            var cancellationEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .Where(e => e.EntryType == InventoryLedgerEntryType.Cancellation)
                .ToListAsync();

            cancellationEntries.Should().HaveCount(1);
            cancellationEntries.First().Quantity.Should().Be(5); // Positive to restore inventory
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task PartialOrderFulfillment_ShouldHandleCorrectly()
        {
            // Integration test for partial order fulfillment scenarios
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItems = new List<InventoryItem>
            {
                await CreateTestInventoryItemAsync("Available Product", quantity: 100),
                await CreateTestInventoryItemAsync("Limited Product", quantity: 3),
                await CreateTestInventoryItemAsync("Out of Stock Product", quantity: 0)
            };

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItems[0].Id,
                    Quantity = 5,
                    UnitPrice = new Money(20.00m, "USD")
                },
                new OrderItem
                {
                    InventoryItemId = inventoryItems[1].Id,
                    Quantity = 2, // Within available stock
                    UnitPrice = new Money(30.00m, "USD")
                },
                new OrderItem
                {
                    InventoryItemId = inventoryItems[2].Id,
                    Quantity = 1, // Out of stock
                    UnitPrice = new Money(40.00m, "USD")
                }
            };

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Attempt to create order with mixed inventory availability
            var exception = await Assert.ThrowsAsync<InsufficientInventoryException>(
                () => OrderService.CreateOrderAsync(
                    customer.Id,
                    orderItems,
                    customer.DefaultShippingAddress));

            // Assert - Verify partial fulfillment handling
            exception.Should().NotBeNull();
            exception.UnavailableItems.Should().HaveCount(1);
            exception.UnavailableItems.First().InventoryItemId.Should().Be(inventoryItems[2].Id);

            // Verify no inventory was reserved for any items
            var finalInventoryStates = await Context.InventoryItems
                .Where(i => inventoryItems.Select(item => item.Id).Contains(i.Id))
                .ToListAsync();

            finalInventoryStates[0].QuantityReserved.Should().Be(0);
            finalInventoryStates[1].QuantityReserved.Should().Be(0);
            finalInventoryStates[2].QuantityReserved.Should().Be(0);

            // Test successful partial order (removing out-of-stock item)
            var availableOrderItems = orderItems.Take(2).ToList();
            
            var partialOrder = await OrderService.CreateOrderAsync(
                customer.Id,
                availableOrderItems,
                customer.DefaultShippingAddress);

            partialOrder.Should().NotBeNull();
            partialOrder.Items.Should().HaveCount(2);
            partialOrder.TotalAmount.Amount.Should().Be(160.00m); // (5*$20) + (2*$30)

            // Verify inventory reservations for successful items
            var reservedInventory = await Context.InventoryItems
                .Where(i => availableOrderItems.Select(item => item.InventoryItemId).Contains(i.Id))
                .ToListAsync();

            reservedInventory[0].QuantityReserved.Should().Be(5);
            reservedInventory[1].QuantityReserved.Should().Be(2);
        }

        #endregion

        #region Payment Processing Integration Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task PaymentFailureRecovery_ShouldHandleGracefully()
        {
            // Integration test for payment failure and recovery scenarios
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var order = await CreateTestOrderAsync(customer.Id, new Money(100.00m, "USD"));

            var failureCount = 0;
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(() =>
                {
                    failureCount++;
                    if (failureCount <= 2)
                    {
                        return CommerceTestUtilities.CreateFailedPaymentResponse("Temporary failure");
                    }
                    return CommerceTestUtilities.CreateSuccessfulPaymentResponse();
                });

            // Act - Attempt payment with retries
            PaymentProcessingException firstException = null;
            PaymentProcessingException secondException = null;
            Payment successfulPayment = null;

            try
            {
                await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            }
            catch (PaymentProcessingException ex)
            {
                firstException = ex;
            }

            try
            {
                await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            }
            catch (PaymentProcessingException ex)
            {
                secondException = ex;
            }

            // Third attempt should succeed
            successfulPayment = await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);

            // Assert - Verify failure handling and recovery
            firstException.Should().NotBeNull();
            firstException.Message.Should().Contain("Temporary failure");
            
            secondException.Should().NotBeNull();
            secondException.Message.Should().Contain("Temporary failure");
            
            successfulPayment.Should().NotBeNull();
            successfulPayment.Status.Should().Be(PaymentStatus.Completed);
            successfulPayment.Amount.Amount.Should().Be(100.00m);

            // Verify payment history
            var paymentHistory = await Context.Payments
                .Where(p => p.OrderId == order.Id)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            paymentHistory.Should().HaveCount(3);
            paymentHistory.Take(2).Should().AllSatisfy(p => p.Status.Should().Be(PaymentStatus.Failed));
            paymentHistory.Last().Status.Should().Be(PaymentStatus.Completed);

            // Verify order status
            var updatedOrder = await Context.Orders.FindAsync(order.Id);
            updatedOrder.PaymentStatus.Should().Be(PaymentStatus.Completed);

            MockPaymentService.Verify(
                x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()),
                Times.Exactly(3));
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task MultiplePaymentMethods_ShouldProcessCorrectly()
        {
            // Integration test for orders with multiple payment methods
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var order = await CreateTestOrderAsync(customer.Id, new Money(200.00m, "USD"));

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Process payments with different methods
            var creditCardPayment = await PaymentService.ProcessOrderPaymentAsync(
                order.Id, PaymentMethod.CreditCard, 120.00m);

            var paypalPayment = await PaymentService.ProcessOrderPaymentAsync(
                order.Id, PaymentMethod.PayPal, 80.00m);

            // Assert - Verify multiple payments
            var allPayments = await Context.Payments
                .Where(p => p.OrderId == order.Id)
                .ToListAsync();

            allPayments.Should().HaveCount(2);
            
            var ccPayment = allPayments.First(p => p.Method == PaymentMethod.CreditCard);
            ccPayment.Amount.Amount.Should().Be(120.00m);
            ccPayment.Status.Should().Be(PaymentStatus.Completed);
            
            var ppPayment = allPayments.First(p => p.Method == PaymentMethod.PayPal);
            ppPayment.Amount.Amount.Should().Be(80.00m);
            ppPayment.Status.Should().Be(PaymentStatus.Completed);

            // Verify total payment amount
            var totalPaid = allPayments.Sum(p => p.Amount.Amount);
            totalPaid.Should().Be(200.00m);

            // Verify order payment status
            var updatedOrder = await Context.Orders.FindAsync(order.Id);
            updatedOrder.PaymentStatus.Should().Be(PaymentStatus.Completed);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task PartialRefund_ShouldProcessCorrectly()
        {
            // Integration test for partial refund scenarios
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var order = await CreateTestOrderAsync(customer.Id, new Money(150.00m, "USD"));

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            MockPaymentService.Setup(x => x.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulRefundResponse());

            // Process initial payment
            var originalPayment = await PaymentService.ProcessOrderPaymentAsync(
                order.Id, PaymentMethod.CreditCard);

            // Act - Process partial refunds
            var firstRefund = await PaymentService.ProcessRefundAsync(
                originalPayment.Id, 50.00m, "Partial return");

            var secondRefund = await PaymentService.ProcessRefundAsync(
                originalPayment.Id, 30.00m, "Additional adjustment");

            // Assert - Verify partial refunds
            var allPayments = await Context.Payments
                .Where(p => p.OrderId == order.Id)
                .ToListAsync();

            allPayments.Should().HaveCount(3); // Original + 2 refunds
            
            var originalPmt = allPayments.First(p => p.Type == PaymentType.Payment);
            originalPmt.Amount.Amount.Should().Be(150.00m);
            originalPmt.Status.Should().Be(PaymentStatus.Completed);
            
            var refunds = allPayments.Where(p => p.Type == PaymentType.Refund).ToList();
            refunds.Should().HaveCount(2);
            refunds.Sum(r => r.Amount.Amount).Should().Be(80.00m);
            refunds.Should().AllSatisfy(r => r.Status.Should().Be(PaymentStatus.Completed));

            // Verify net payment amount
            var netAmount = originalPmt.Amount.Amount - refunds.Sum(r => r.Amount.Amount);
            netAmount.Should().Be(70.00m);

            // Verify refund references
            refunds.Should().AllSatisfy(r => r.OriginalPaymentId.Should().Be(originalPayment.Id));

            MockPaymentService.Verify(
                x => x.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()),
                Times.Exactly(2));
        }

        #endregion

        #region Inventory Management Integration Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task InventoryReservationWorkflow_ShouldMaintainConsistency()
        {
            // Integration test for inventory reservation throughout order lifecycle
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 15,
                    UnitPrice = new Money(25.00m, "USD")
                }
            };

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act & Assert - Track inventory through order lifecycle
            
            // Step 1: Create order (should reserve inventory)
            var order = await OrderService.CreateOrderAsync(
                customer.Id,
                orderItems,
                customer.DefaultShippingAddress);

            var afterOrderCreation = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            afterOrderCreation.QuantityOnHand.Should().Be(100);
            afterOrderCreation.QuantityReserved.Should().Be(15);
            afterOrderCreation.QuantityAvailable.Should().Be(85);

            // Step 2: Process payment (inventory should remain reserved)
            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);

            var afterPayment = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            afterPayment.QuantityOnHand.Should().Be(100);
            afterPayment.QuantityReserved.Should().Be(15);
            afterPayment.QuantityAvailable.Should().Be(85);

            // Step 3: Process order (should allocate inventory)
            await OrderService.ProcessOrderAsync(order.Id);

            var afterProcessing = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            afterProcessing.QuantityOnHand.Should().Be(85); // Reduced by sale
            afterProcessing.QuantityReserved.Should().Be(0); // No longer reserved
            afterProcessing.QuantityAvailable.Should().Be(85);

            // Verify inventory ledger entries
            var ledgerEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            ledgerEntries.Should().HaveCount(2);
            
            var reservationEntry = ledgerEntries.First();
            reservationEntry.EntryType.Should().Be(InventoryLedgerEntryType.Reserved);
            reservationEntry.Quantity.Should().Be(-15);
            
            var saleEntry = ledgerEntries.Last();
            saleEntry.EntryType.Should().Be(InventoryLedgerEntryType.Sale);
            saleEntry.Quantity.Should().Be(-15);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ConcurrentInventoryOperations_ShouldMaintainConsistency()
        {
            // Integration test for concurrent inventory operations
            
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 1000);
            var customers = new List<Customer>();
            
            for (int i = 0; i < 10; i++)
            {
                customers.Add(await CreateTestCustomerAsync());
            }

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Create concurrent orders
            var concurrentOrderTasks = customers.Select(async customer =>
            {
                var orderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        InventoryItemId = inventoryItem.Id,
                        Quantity = 50,
                        UnitPrice = new Money(20.00m, "USD")
                    }
                };

                try
                {
                    var order = await OrderService.CreateOrderAsync(
                        customer.Id,
                        orderItems,
                        customer.DefaultShippingAddress);

                    await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
                    await OrderService.ProcessOrderAsync(order.Id);
                    
                    return new { Success = true, OrderId = order.Id, Error = (string)null };
                }
                catch (Exception ex)
                {
                    return new { Success = false, OrderId = Guid.Empty, Error = ex.Message };
                }
            });

            var results = await Task.WhenAll(concurrentOrderTasks);

            // Assert - Verify inventory consistency
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            var successfulOrders = results.Count(r => r.Success);
            var expectedFinalQuantity = 1000 - (successfulOrders * 50);
            
            finalInventory.QuantityOnHand.Should().Be(expectedFinalQuantity);
            finalInventory.QuantityReserved.Should().Be(0);
            finalInventory.QuantityAvailable.Should().Be(expectedFinalQuantity);

            // Verify that we didn't oversell
            successfulOrders.Should().BeLessOrEqualTo(20); // Max 20 orders of 50 items each
            
            // Verify ledger entries match successful orders
            var saleEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .Where(e => e.EntryType == InventoryLedgerEntryType.Sale)
                .ToListAsync();

            saleEntries.Should().HaveCount(successfulOrders);
            saleEntries.Sum(e => Math.Abs(e.Quantity)).Should().Be(successfulOrders * 50);

            // Verify orders were created correctly
            var createdOrders = await Context.Orders
                .Where(o => results.Where(r => r.Success).Select(r => r.OrderId).Contains(o.Id))
                .ToListAsync();

            createdOrders.Should().HaveCount(successfulOrders);
            createdOrders.Should().AllSatisfy(o => o.Status.Should().Be(OrderStatus.Processing));
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task InventoryAdjustments_ShouldIntegrateWithOrders()
        {
            // Integration test for inventory adjustments affecting order processing
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 50);

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 30,
                    UnitPrice = new Money(25.00m, "USD")
                }
            };

            // Act - Create order, then adjust inventory, then try to process
            var order = await OrderService.CreateOrderAsync(
                customer.Id,
                orderItems,
                customer.DefaultShippingAddress);

            // Verify initial reservation
            var afterOrder = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            afterOrder.QuantityReserved.Should().Be(30);
            afterOrder.QuantityAvailable.Should().Be(20);

            // Perform inventory adjustment (damage/loss)
            await InventoryService.AdjustInventoryAsync(
                inventoryItem.Id,
                -25, // Reduce by 25
                InventoryAdjustmentReason.Damage,
                "Damaged goods");

            // Verify adjustment impact
            var afterAdjustment = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            afterAdjustment.QuantityOnHand.Should().Be(25); // 50 - 25
            afterAdjustment.QuantityReserved.Should().Be(30); // Still reserved
            afterAdjustment.QuantityAvailable.Should().Be(-5); // Negative available!

            // Try to process the order (should handle negative availability)
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            
            // Processing should fail due to insufficient inventory after adjustment
            var exception = await Assert.ThrowsAsync<InsufficientInventoryException>(
                () => OrderService.ProcessOrderAsync(order.Id));

            exception.Should().NotBeNull();
            exception.Message.Should().Contain("Insufficient inventory");

            // Verify order status
            var finalOrder = await Context.Orders.FindAsync(order.Id);
            finalOrder.Status.Should().Be(OrderStatus.Pending); // Should remain pending
            finalOrder.PaymentStatus.Should().Be(PaymentStatus.Completed); // Payment succeeded

            // Verify inventory ledger shows adjustment
            var adjustmentEntry = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .Where(e => e.EntryType == InventoryLedgerEntryType.Adjustment)
                .FirstOrDefaultAsync();

            adjustmentEntry.Should().NotBeNull();
            adjustmentEntry.Quantity.Should().Be(-25);
            adjustmentEntry.Reference.Should().Contain("Damaged goods");
        }

        #endregion

        #region Subscription Integration Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task SubscriptionLifecycle_ShouldProcessCorrectly()
        {
            // Integration test for complete subscription lifecycle
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var plan = await CreateTestSubscriptionPlanAsync(
                "Premium Plan",
                new Money(29.99m, "USD"),
                BillingCycle.Monthly);

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            MockNotificationService.Setup(x => x.SendSubscriptionWelcomeAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            MockNotificationService.Setup(x => x.SendSubscriptionRenewalAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act - Execute subscription lifecycle
            
            // Step 1: Create subscription
            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id,
                plan.Id,
                DateTime.UtcNow);

            // Step 2: Activate subscription
            await SubscriptionService.ActivateSubscriptionAsync(subscription.Id);

            // Step 3: Process first billing
            var firstBilling = await SubscriptionService.ProcessBillingAsync(
                subscription.Id,
                DateTime.UtcNow);

            // Step 4: Simulate time passage and renewal
            var renewalDate = DateTime.UtcNow.AddMonths(1);
            var renewal = await SubscriptionService.ProcessRenewalAsync(
                subscription.Id,
                renewalDate);

            // Step 5: Pause subscription
            await SubscriptionService.PauseSubscriptionAsync(
                subscription.Id,
                "Customer request");

            // Step 6: Resume subscription
            await SubscriptionService.ResumeSubscriptionAsync(subscription.Id);

            // Step 7: Cancel subscription
            await SubscriptionService.CancelSubscriptionAsync(
                subscription.Id,
                DateTime.UtcNow.AddDays(5),
                "Customer cancellation");

            // Assert - Verify complete lifecycle
            var finalSubscription = await Context.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscription.Id);

            finalSubscription.Should().NotBeNull();
            finalSubscription.Status.Should().Be(SubscriptionStatus.Cancelled);
            finalSubscription.CancelledAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(5), TimeSpan.FromMinutes(1));
            finalSubscription.CancellationReason.Should().Be("Customer cancellation");

            // Verify billing history
            var billingHistory = await Context.Payments
                .Where(p => p.SubscriptionId == subscription.Id)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            billingHistory.Should().HaveCount(2); // Initial + renewal
            billingHistory.Should().AllSatisfy(p =>
            {
                p.Amount.Amount.Should().Be(29.99m);
                p.Status.Should().Be(PaymentStatus.Completed);
                p.Type.Should().Be(PaymentType.Payment);
            });

            // Verify subscription events were logged
            var subscriptionEvents = await Context.SubscriptionEvents
                .Where(e => e.SubscriptionId == subscription.Id)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            subscriptionEvents.Should().Contain(e => e.EventType == SubscriptionEventType.Created);
            subscriptionEvents.Should().Contain(e => e.EventType == SubscriptionEventType.Activated);
            subscriptionEvents.Should().Contain(e => e.EventType == SubscriptionEventType.Renewed);
            subscriptionEvents.Should().Contain(e => e.EventType == SubscriptionEventType.Paused);
            subscriptionEvents.Should().Contain(e => e.EventType == SubscriptionEventType.Resumed);
            subscriptionEvents.Should().Contain(e => e.EventType == SubscriptionEventType.Cancelled);

            // Verify notifications were sent
            MockNotificationService.Verify(
                x => x.SendSubscriptionWelcomeAsync(subscription.Id),
                Times.Once);
            
            MockNotificationService.Verify(
                x => x.SendSubscriptionRenewalAsync(subscription.Id),
                Times.Once);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task SubscriptionBillingFailure_ShouldHandleGracefully()
        {
            // Integration test for subscription billing failure scenarios
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var plan = await CreateTestSubscriptionPlanAsync(
                "Basic Plan",
                new Money(19.99m, "USD"),
                BillingCycle.Monthly);

            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id,
                plan.Id,
                DateTime.UtcNow);

            await SubscriptionService.ActivateSubscriptionAsync(subscription.Id);

            // Setup payment failure
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateFailedPaymentResponse("Insufficient funds"));

            MockNotificationService.Setup(x => x.SendPaymentFailureNotificationAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act - Attempt billing with failure
            var billingException = await Assert.ThrowsAsync<PaymentProcessingException>(
                () => SubscriptionService.ProcessBillingAsync(subscription.Id, DateTime.UtcNow));

            // Assert - Verify failure handling
            billingException.Should().NotBeNull();
            billingException.Message.Should().Contain("Insufficient funds");

            // Verify subscription status
            var failedSubscription = await Context.Subscriptions.FindAsync(subscription.Id);
            failedSubscription.Status.Should().Be(SubscriptionStatus.PastDue);
            failedSubscription.LastBillingAttempt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            failedSubscription.BillingRetryCount.Should().Be(1);

            // Verify failed payment record
            var failedPayment = await Context.Payments
                .Where(p => p.SubscriptionId == subscription.Id)
                .FirstOrDefaultAsync();

            failedPayment.Should().NotBeNull();
            failedPayment.Status.Should().Be(PaymentStatus.Failed);
            failedPayment.FailureReason.Should().Be("Insufficient funds");

            // Verify notification was sent
            MockNotificationService.Verify(
                x => x.SendPaymentFailureNotificationAsync(subscription.Id),
                Times.Once);

            // Test successful retry
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            var retryResult = await SubscriptionService.ProcessBillingAsync(
                subscription.Id,
                DateTime.UtcNow.AddDays(1));

            retryResult.Should().BeTrue();

            // Verify subscription recovery
            var recoveredSubscription = await Context.Subscriptions.FindAsync(subscription.Id);
            recoveredSubscription.Status.Should().Be(SubscriptionStatus.Active);
            recoveredSubscription.BillingRetryCount.Should().Be(0);

            // Verify successful payment
            var successfulPayment = await Context.Payments
                .Where(p => p.SubscriptionId == subscription.Id)
                .Where(p => p.Status == PaymentStatus.Completed)
                .FirstOrDefaultAsync();

            successfulPayment.Should().NotBeNull();
            successfulPayment.Amount.Amount.Should().Be(19.99m);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task SubscriptionPlanUpgrade_ShouldProcessCorrectly()
        {
            // Integration test for subscription plan upgrades
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            
            var basicPlan = await CreateTestSubscriptionPlanAsync(
                "Basic Plan",
                new Money(9.99m, "USD"),
                BillingCycle.Monthly);
            
            var premiumPlan = await CreateTestSubscriptionPlanAsync(
                "Premium Plan",
                new Money(19.99m, "USD"),
                BillingCycle.Monthly);

            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id,
                basicPlan.Id,
                DateTime.UtcNow);

            await SubscriptionService.ActivateSubscriptionAsync(subscription.Id);

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Process initial billing
            await SubscriptionService.ProcessBillingAsync(subscription.Id, DateTime.UtcNow);

            // Act - Upgrade subscription plan
            var upgradeDate = DateTime.UtcNow.AddDays(15); // Mid-cycle upgrade
            await SubscriptionService.UpgradeSubscriptionAsync(
                subscription.Id,
                premiumPlan.Id,
                upgradeDate);

            // Assert - Verify upgrade
            var upgradedSubscription = await Context.Subscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscription.Id);

            upgradedSubscription.Should().NotBeNull();
            upgradedSubscription.PlanId.Should().Be(premiumPlan.Id);
            upgradedSubscription.Plan.Name.Should().Be("Premium Plan");
            upgradedSubscription.Status.Should().Be(SubscriptionStatus.Active);

            // Verify prorated billing
            var payments = await Context.Payments
                .Where(p => p.SubscriptionId == subscription.Id)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            payments.Should().HaveCount(2); // Initial + upgrade proration
            
            var initialPayment = payments.First();
            initialPayment.Amount.Amount.Should().Be(9.99m);
            
            var prorationPayment = payments.Last();
            prorationPayment.Type.Should().Be(PaymentType.Payment);
            prorationPayment.Amount.Amount.Should().BeGreaterThan(0); // Prorated amount
            prorationPayment.Reference.Should().Contain("upgrade");

            // Verify subscription events
            var upgradeEvent = await Context.SubscriptionEvents
                .Where(e => e.SubscriptionId == subscription.Id)
                .Where(e => e.EventType == SubscriptionEventType.PlanChanged)
                .FirstOrDefaultAsync();

            upgradeEvent.Should().NotBeNull();
            upgradeEvent.Details.Should().Contain("Premium Plan");

            // Verify next billing uses new plan price
            var nextBillingDate = upgradedSubscription.CurrentPeriodEnd;
            await SubscriptionService.ProcessBillingAsync(subscription.Id, nextBillingDate);

            var nextBillingPayment = await Context.Payments
                .Where(p => p.SubscriptionId == subscription.Id)
                .Where(p => p.CreatedAt > prorationPayment.CreatedAt)
                .FirstOrDefaultAsync();

            nextBillingPayment.Should().NotBeNull();
            nextBillingPayment.Amount.Amount.Should().Be(19.99m); // New plan price
        }

        #endregion

        #region Cross-Service Integration Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Integration)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task OrderToSubscriptionConversion_ShouldProcessCorrectly()
        {
            // Integration test for converting one-time orders to subscriptions
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var subscriptionPlan = await CreateTestSubscriptionPlanAsync(
                "Monthly Service",
                new Money(39.99m, "USD"),
                BillingCycle.Monthly);

            // Create initial one-time order
            var order = await CreateTestOrderAsync(customer.Id, new Money(39.99m, "USD"));

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            await OrderService.ProcessOrderAsync(order.Id);
            await OrderService.CompleteOrderAsync(order.Id);

            // Act - Convert order to subscription
            var subscription = await SubscriptionService.ConvertOrderToSubscriptionAsync(
                order.Id,
                subscriptionPlan.Id,
                DateTime.UtcNow);

            // Assert - Verify conversion
            subscription.Should().NotBeNull();
            subscription.CustomerId.Should().Be(customer.Id);
            subscription.PlanId.Should().Be(subscriptionPlan.Id);
            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.OriginalOrderId.Should().Be(order.Id);

            // Verify order is marked as converted
            var convertedOrder = await Context.Orders.FindAsync(order.Id);
            convertedOrder.ConvertedToSubscriptionId.Should().Be(subscription.Id);
            convertedOrder.IsConvertedToSubscription.Should().BeTrue();

            // Verify subscription billing starts correctly
            var firstBilling = await SubscriptionService.ProcessBillingAsync(
                subscription.Id,
                DateTime.UtcNow.AddMonths(1));

            firstBilling.Should().BeTrue();

            var subscriptionPayment = await Context.Payments
                .Where(p => p.SubscriptionId == subscription.Id)
                .FirstOrDefaultAsync();

            subscriptionPayment.Should().NotBeNull();
            subscriptionPayment.Amount.Amount.Should().Be(39.99m);
            subscriptionPayment.Status.Should().Be(PaymentStatus.Completed);

            // Verify conversion event
            var conversionEvent = await Context.SubscriptionEvents
                .Where(e => e.SubscriptionId == subscription.Id)
                .Where(e => e.EventType == SubscriptionEventType.ConvertedFromOrder)
                .FirstOrDefaultAsync();

            conversionEvent.Should().NotBeNull();
            conversionEvent.Details.Should().Contain(order.Id.ToString());
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Integration)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task InventorySubscriptionIntegration_ShouldAllocateCorrectly()
        {
            // Integration test for subscription-based inventory allocation
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync("Subscription Product", quantity: 1000);
            
            var subscriptionPlan = await CreateTestSubscriptionPlanAsync(
                "Product Subscription",
                new Money(29.99m, "USD"),
                BillingCycle.Monthly);

            // Associate inventory item with subscription plan
            var planInventoryItem = new SubscriptionPlanInventoryItem
            {
                SubscriptionPlanId = subscriptionPlan.Id,
                InventoryItemId = inventoryItem.Id,
                QuantityPerBilling = 5
            };
            
            Context.SubscriptionPlanInventoryItems.Add(planInventoryItem);
            await Context.SaveChangesAsync();

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Create and process subscription
            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id,
                subscriptionPlan.Id,
                DateTime.UtcNow);

            await SubscriptionService.ActivateSubscriptionAsync(subscription.Id);
            
            // Process multiple billing cycles
            for (int i = 0; i < 3; i++)
            {
                var billingDate = DateTime.UtcNow.AddMonths(i);
                await SubscriptionService.ProcessBillingAsync(subscription.Id, billingDate);
            }

            // Assert - Verify inventory allocation
            var finalInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventory.QuantityOnHand.Should().Be(985); // 1000 - (3 * 5)

            // Verify subscription inventory allocations
            var allocations = await Context.SubscriptionInventoryAllocations
                .Where(a => a.SubscriptionId == subscription.Id)
                .ToListAsync();

            allocations.Should().HaveCount(3);
            allocations.Should().AllSatisfy(a =>
            {
                a.InventoryItemId.Should().Be(inventoryItem.Id);
                a.QuantityAllocated.Should().Be(5);
            });

            // Verify inventory ledger entries
            var subscriptionEntries = await Context.InventoryLedgerEntries
                .Where(e => e.InventoryItemId == inventoryItem.Id)
                .Where(e => e.EntryType == InventoryLedgerEntryType.SubscriptionAllocation)
                .ToListAsync();

            subscriptionEntries.Should().HaveCount(3);
            subscriptionEntries.Sum(e => Math.Abs(e.Quantity)).Should().Be(15);

            // Test subscription cancellation inventory restoration
            await SubscriptionService.CancelSubscriptionAsync(
                subscription.Id,
                DateTime.UtcNow,
                "Test cancellation");

            // Verify no additional inventory is allocated after cancellation
            var postCancellationInventory = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            postCancellationInventory.QuantityOnHand.Should().Be(985); // Should remain the same

            // Attempt billing after cancellation (should not allocate inventory)
            var postCancellationBilling = await SubscriptionService.ProcessBillingAsync(
                subscription.Id,
                DateTime.UtcNow.AddMonths(4));

            postCancellationBilling.Should().BeFalse();
            
            var finalInventoryAfterCancellation = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            finalInventoryAfterCancellation.QuantityOnHand.Should().Be(985); // No change
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Integration)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task CustomerLoyaltyIntegration_ShouldApplyDiscounts()
        {
            // Integration test for customer loyalty program integration
            
            // Arrange
            var customer = await CreateTestCustomerAsync();
            
            // Create customer loyalty profile
            var loyaltyProfile = new CustomerLoyaltyProfile
            {
                CustomerId = customer.Id,
                TierLevel = LoyaltyTier.Gold,
                PointsBalance = 1000,
                LifetimeSpent = 2500.00m,
                DiscountPercentage = 15.0m
            };
            
            Context.CustomerLoyaltyProfiles.Add(loyaltyProfile);
            await Context.SaveChangesAsync();

            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 2,
                    UnitPrice = new Money(100.00m, "USD")
                }
            };

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act - Create order with loyalty discount
            var order = await OrderService.CreateOrderAsync(
                customer.Id,
                orderItems,
                customer.DefaultShippingAddress);

            // Apply loyalty discount
            await OrderService.ApplyLoyaltyDiscountAsync(order.Id);

            // Process order
            await PaymentService.ProcessOrderPaymentAsync(order.Id, PaymentMethod.CreditCard);
            await OrderService.ProcessOrderAsync(order.Id);
            await OrderService.CompleteOrderAsync(order.Id);

            // Assert - Verify discount application
            var discountedOrder = await Context.Orders
                .Include(o => o.Discounts)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            discountedOrder.Should().NotBeNull();
            discountedOrder.SubtotalAmount.Amount.Should().Be(200.00m); // 2 * $100
            discountedOrder.DiscountAmount.Amount.Should().Be(30.00m); // 15% of $200
            discountedOrder.TotalAmount.Amount.Should().Be(170.00m); // $200 - $30

            // Verify loyalty discount record
            var loyaltyDiscount = discountedOrder.Discounts
                .FirstOrDefault(d => d.Type == DiscountType.Loyalty);
            
            loyaltyDiscount.Should().NotBeNull();
            loyaltyDiscount.Amount.Amount.Should().Be(30.00m);
            loyaltyDiscount.Percentage.Should().Be(15.0m);

            // Verify loyalty points earned
            var updatedLoyaltyProfile = await Context.CustomerLoyaltyProfiles
                .FirstOrDefaultAsync(p => p.CustomerId == customer.Id);

            updatedLoyaltyProfile.Should().NotBeNull();
            updatedLoyaltyProfile.PointsBalance.Should().Be(1017); // 1000 + (170 * 0.1 points per dollar)
            updatedLoyaltyProfile.LifetimeSpent.Should().Be(2670.00m); // 2500 + 170

            // Verify payment amount reflects discount
            var payment = await Context.Payments
                .Where(p => p.OrderId == order.Id)
                .FirstOrDefaultAsync();

            payment.Should().NotBeNull();
            payment.Amount.Amount.Should().Be(170.00m); // Discounted amount
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Integration)]
        [Trait(TestTraits.Priority, TestPriorities.Low)]
        public async Task MultiTenantDataIsolation_ShouldMaintainSeparation()
        {
            // Integration test for multi-tenant data isolation
            
            // Arrange - Create data for different tenants
            var tenant1Id = Guid.NewGuid();
            var tenant2