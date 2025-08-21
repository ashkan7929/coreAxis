using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.ValueObjects;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Exceptions;
using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace CoreAxis.Modules.CommerceModule.Tests.Unit
{
    /// <summary>
    /// Unit tests for Commerce Module components
    /// Tests individual components in isolation with mocked dependencies
    /// </summary>
    [Trait(TestTraits.Category, TestCategories.Unit)]
    public class CommerceUnitTests : CommerceTestBase
    {
        #region Value Objects Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.ValueObjects)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public void Money_WithValidAmount_ShouldCreateSuccessfully()
        {
            // Arrange
            var amount = 100.50m;
            var currency = "USD";

            // Act
            var money = new Money(amount, currency);

            // Assert
            money.Amount.Should().Be(amount);
            money.Currency.Should().Be(currency);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.ValueObjects)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public void Money_WithNegativeAmount_ShouldThrowException()
        {
            // Arrange
            var amount = -10.00m;
            var currency = "USD";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Money(amount, currency));
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.ValueObjects)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public void Money_Addition_ShouldCalculateCorrectly()
        {
            // Arrange
            var money1 = new Money(100.00m, "USD");
            var money2 = new Money(50.00m, "USD");

            // Act
            var result = money1 + money2;

            // Assert
            result.Amount.Should().Be(150.00m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.ValueObjects)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public void Money_AdditionWithDifferentCurrencies_ShouldThrowException()
        {
            // Arrange
            var money1 = new Money(100.00m, "USD");
            var money2 = new Money(50.00m, "EUR");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => money1 + money2);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.ValueObjects)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public void Address_WithValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var street = "123 Main St";
            var city = "New York";
            var state = "NY";
            var zipCode = "10001";
            var country = "USA";

            // Act
            var address = new Address(street, city, state, zipCode, country);

            // Assert
            address.Street.Should().Be(street);
            address.City.Should().Be(city);
            address.State.Should().Be(state);
            address.ZipCode.Should().Be(zipCode);
            address.Country.Should().Be(country);
        }

        #endregion

        #region Inventory Management Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task ReserveInventory_WithSufficientStock_ShouldSucceed()
        {
            // Arrange
            var inventoryItem = CommerceTestUtilities.GetInventoryItemFaker()
                .RuleFor(i => i.QuantityOnHand, 100)
                .Generate();
            
            await Context.InventoryItems.AddAsync(inventoryItem);
            await Context.SaveChangesAsync();

            var reservationQuantity = 10;

            // Act
            var result = await InventoryService.ReserveAsync(
                inventoryItem.Id, 
                reservationQuantity, 
                "Test Order");

            // Assert
            result.Should().BeTrue();
            
            var updatedItem = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            updatedItem.QuantityReserved.Should().Be(reservationQuantity);
            updatedItem.QuantityAvailable.Should().Be(90);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task ReserveInventory_WithInsufficientStock_ShouldFail()
        {
            // Arrange
            var inventoryItem = CommerceTestUtilities.GetInventoryItemFaker()
                .RuleFor(i => i.QuantityOnHand, 5)
                .Generate();
            
            await Context.InventoryItems.AddAsync(inventoryItem);
            await Context.SaveChangesAsync();

            var reservationQuantity = 10;

            // Act
            var result = await InventoryService.ReserveAsync(
                inventoryItem.Id, 
                reservationQuantity, 
                "Test Order");

            // Assert
            result.Should().BeFalse();
            
            var updatedItem = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            updatedItem.QuantityReserved.Should().Be(0);
            updatedItem.QuantityAvailable.Should().Be(5);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ReleaseReservation_WithValidReservation_ShouldSucceed()
        {
            // Arrange
            var inventoryItem = CommerceTestUtilities.GetInventoryItemFaker()
                .RuleFor(i => i.QuantityOnHand, 100)
                .RuleFor(i => i.QuantityReserved, 20)
                .Generate();
            
            await Context.InventoryItems.AddAsync(inventoryItem);
            await Context.SaveChangesAsync();

            var releaseQuantity = 10;

            // Act
            await InventoryService.ReleaseReservationAsync(
                inventoryItem.Id, 
                releaseQuantity, 
                "Test Release");

            // Assert
            var updatedItem = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            updatedItem.QuantityReserved.Should().Be(10);
            updatedItem.QuantityAvailable.Should().Be(90);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task UpdateStock_WithValidQuantity_ShouldUpdateCorrectly()
        {
            // Arrange
            var inventoryItem = CommerceTestUtilities.GetInventoryItemFaker()
                .RuleFor(i => i.QuantityOnHand, 50)
                .Generate();
            
            await Context.InventoryItems.AddAsync(inventoryItem);
            await Context.SaveChangesAsync();

            var newQuantity = 75;

            // Act
            await InventoryService.UpdateStockAsync(
                inventoryItem.Id, 
                newQuantity, 
                "Stock Adjustment");

            // Assert
            var updatedItem = await Context.InventoryItems.FindAsync(inventoryItem.Id);
            updatedItem.QuantityOnHand.Should().Be(newQuantity);
        }

        #endregion

        #region Order Processing Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task CreateOrder_WithValidData_ShouldSucceed()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 2,
                    UnitPrice = new Money(25.00m, "USD")
                }
            };

            // Act
            var order = await OrderService.CreateOrderAsync(
                customer.Id, 
                orderItems, 
                customer.DefaultShippingAddress);

            // Assert
            order.Should().NotBeNull();
            order.CustomerId.Should().Be(customer.Id);
            order.Status.Should().Be(OrderStatus.Pending);
            order.Items.Should().HaveCount(1);
            order.TotalAmount.Amount.Should().Be(50.00m);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task ProcessOrder_WithValidOrder_ShouldUpdateStatus()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            
            // Mock payment service to return success
            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act
            await OrderService.ProcessOrderAsync(order.Id);

            // Assert
            var updatedOrder = await Context.Orders.FindAsync(order.Id);
            updatedOrder.Status.Should().Be(OrderStatus.Processing);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task CancelOrder_WithPendingOrder_ShouldSucceed()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            order.Status = OrderStatus.Pending;
            await Context.SaveChangesAsync();

            // Act
            await OrderService.CancelOrderAsync(order.Id, "Customer request");

            // Assert
            var updatedOrder = await Context.Orders.FindAsync(order.Id);
            updatedOrder.Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task CancelOrder_WithShippedOrder_ShouldThrowException()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            order.Status = OrderStatus.Shipped;
            await Context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => OrderService.CancelOrderAsync(order.Id, "Customer request"));
        }

        #endregion

        #region Pricing and Discounts Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Pricing)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task CalculateOrderTotal_WithoutDiscounts_ShouldCalculateCorrectly()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            
            // Act
            var total = await PricingService.CalculateOrderTotalAsync(order.Id);

            // Assert
            total.Should().NotBeNull();
            total.Amount.Should().BeGreaterThan(0);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Pricing)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task ApplyDiscount_WithValidCoupon_ShouldReduceTotal()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            var discountRule = await CreateTestDiscountRuleAsync(discountPercentage: 10);
            var coupon = await CreateTestCouponAsync(discountRule.Id);
            
            var originalTotal = await PricingService.CalculateOrderTotalAsync(order.Id);

            // Act
            var discountedTotal = await PricingService.ApplyDiscountAsync(
                order.Id, 
                coupon.Code);

            // Assert
            discountedTotal.Amount.Should().BeLessThan(originalTotal.Amount);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Pricing)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ApplyDiscount_WithExpiredCoupon_ShouldThrowException()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            var discountRule = await CreateTestDiscountRuleAsync();
            var coupon = await CreateTestCouponAsync(
                discountRule.Id, 
                expiryDate: DateTime.UtcNow.AddDays(-1));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => PricingService.ApplyDiscountAsync(order.Id, coupon.Code));
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Pricing)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ApplyDiscount_WithUsedCoupon_ShouldThrowException()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            var discountRule = await CreateTestDiscountRuleAsync();
            var coupon = await CreateTestCouponAsync(
                discountRule.Id, 
                usageLimit: 1, 
                usageCount: 1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => PricingService.ApplyDiscountAsync(order.Id, coupon.Code));
        }

        #endregion

        #region Subscription Management Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task CreateSubscription_WithValidPlan_ShouldSucceed()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var plan = await CreateTestSubscriptionPlanAsync();

            // Act
            var subscription = await SubscriptionService.CreateSubscriptionAsync(
                customer.Id, 
                plan.Id, 
                DateTime.UtcNow);

            // Assert
            subscription.Should().NotBeNull();
            subscription.CustomerId.Should().Be(customer.Id);
            subscription.PlanId.Should().Be(plan.Id);
            subscription.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task RenewSubscription_WithActiveSubscription_ShouldExtendPeriod()
        {
            // Arrange
            var subscription = await CreateTestSubscriptionAsync();
            var originalEndDate = subscription.CurrentPeriodEnd;

            // Act
            await SubscriptionService.RenewSubscriptionAsync(subscription.Id);

            // Assert
            var updatedSubscription = await Context.Subscriptions.FindAsync(subscription.Id);
            updatedSubscription.CurrentPeriodEnd.Should().BeAfter(originalEndDate);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Subscriptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task CancelSubscription_WithActiveSubscription_ShouldUpdateStatus()
        {
            // Arrange
            var subscription = await CreateTestSubscriptionAsync();

            // Act
            await SubscriptionService.CancelSubscriptionAsync(
                subscription.Id, 
                "Customer request");

            // Assert
            var updatedSubscription = await Context.Subscriptions.FindAsync(subscription.Id);
            updatedSubscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        }

        #endregion

        #region Payment Processing Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task ProcessPayment_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            var paymentRequest = new PaymentRequest
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentMethod = PaymentMethod.CreditCard,
                PaymentDetails = new Dictionary<string, object>
                {
                    { "cardNumber", "4111111111111111" },
                    { "expiryMonth", "12" },
                    { "expiryYear", "2025" },
                    { "cvv", "123" }
                }
            };

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulPaymentResponse());

            // Act
            var result = await MockPaymentService.Object.ProcessPaymentAsync(paymentRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.TransactionId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Payments)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ProcessPayment_WithInvalidCard_ShouldFail()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            var paymentRequest = new PaymentRequest
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentMethod = PaymentMethod.CreditCard,
                PaymentDetails = new Dictionary<string, object>
                {
                    { "cardNumber", "4000000000000002" }, // Declined card
                    { "expiryMonth", "12" },
                    { "expiryYear", "2025" },
                    { "cvv", "123" }
                }
            };

            MockPaymentService.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateFailedPaymentResponse("Card declined"));

            // Act
            var result = await MockPaymentService.Object.ProcessPaymentAsync(paymentRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("declined");
        }

        #endregion

        #region Refund Processing Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Refunds)]
        [Trait(TestTraits.Priority, TestPriorities.High)]
        public async Task CreateRefundRequest_WithValidOrder_ShouldSucceed()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            order.Status = OrderStatus.Completed;
            await Context.SaveChangesAsync();

            var refundAmount = new Money(25.00m, "USD");

            // Act
            var refundRequest = await RefundService.CreateRefundRequestAsync(
                order.Id, 
                refundAmount, 
                "Customer not satisfied");

            // Assert
            refundRequest.Should().NotBeNull();
            refundRequest.OrderId.Should().Be(order.Id);
            refundRequest.Amount.Should().Be(refundAmount);
            refundRequest.Status.Should().Be(RefundStatus.Pending);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Refunds)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task ProcessRefund_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var refundRequest = await CreateTestRefundRequestAsync();
            
            MockPaymentService.Setup(x => x.ProcessRefundAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync(CommerceTestUtilities.CreateSuccessfulRefundResponse());

            // Act
            await RefundService.ProcessRefundAsync(refundRequest.Id);

            // Assert
            var updatedRequest = await Context.RefundRequests.FindAsync(refundRequest.Id);
            updatedRequest.Status.Should().Be(RefundStatus.Completed);
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Refunds)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task CreateRefundRequest_WithExcessiveAmount_ShouldThrowException()
        {
            // Arrange
            var order = await CreateTestOrderAsync();
            order.Status = OrderStatus.Completed;
            await Context.SaveChangesAsync();

            var excessiveAmount = new Money(order.TotalAmount.Amount + 100, "USD");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => RefundService.CreateRefundRequestAsync(
                    order.Id, 
                    excessiveAmount, 
                    "Test refund"));
        }

        #endregion

        #region Domain Exceptions Tests

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Exceptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public void InsufficientInventoryException_ShouldContainCorrectDetails()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var requested = 10;
            var available = 5;

            // Act
            var exception = new InsufficientInventoryException(itemId, requested, available);

            // Assert
            exception.ItemId.Should().Be(itemId);
            exception.RequestedQuantity.Should().Be(requested);
            exception.AvailableQuantity.Should().Be(available);
            exception.Message.Should().Contain("Insufficient inventory");
        }

        [Fact]
        [Trait(TestTraits.Feature, CommerceFeatures.Exceptions)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public void InvalidOrderStateException_ShouldContainCorrectDetails()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var currentStatus = OrderStatus.Shipped;
            var operation = "cancel";

            // Act
            var exception = new InvalidOrderStateException(orderId, currentStatus, operation);

            // Assert
            exception.OrderId.Should().Be(orderId);
            exception.CurrentStatus.Should().Be(currentStatus);
            exception.AttemptedOperation.Should().Be(operation);
            exception.Message.Should().Contain("Invalid order state");
        }

        #endregion

        #region Performance Validation Tests

        [Fact]
        [Trait(TestTraits.Category, TestCategories.Performance)]
        [Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task InventoryReservation_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 1000);
            var timeout = TimeSpan.FromMilliseconds(100);

            // Act & Assert
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var result = await InventoryService.ReserveAsync(
                inventoryItem.Id, 
                10, 
                "Performance test");
            
            stopwatch.Stop();

            result.Should().BeTrue();
            stopwatch.Elapsed.Should().BeLessThan(timeout);
        }

        [Fact]
        [Trait(TestTraits.Category, TestCategories.Performance)]
        [Trait(TestTraits.Feature, CommerceFeatures.Orders)]
        [Trait(TestTraits.Priority, TestPriorities.Medium)]
        public async Task OrderCreation_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var customer = await CreateTestCustomerAsync();
            var inventoryItem = await CreateTestInventoryItemAsync(quantity: 100);
            var timeout = TimeSpan.FromMilliseconds(200);
            
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    InventoryItemId = inventoryItem.Id,
                    Quantity = 1,
                    UnitPrice = new Money(10.00m, "USD")
                }
            };

            // Act & Assert
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var order = await OrderService.CreateOrderAsync(
                customer.Id, 
                orderItems, 
                customer.DefaultShippingAddress);
            
            stopwatch.Stop();

            order.Should().NotBeNull();
            stopwatch.Elapsed.Should().BeLessThan(timeout);
        }

        #endregion
    }
}