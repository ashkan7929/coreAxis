using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Infrastructure.ExternalServices;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CoreAxis.Modules.CommerceModule.Tests.EndToEnd;

public class CommerceE2ETests : IClassFixture<CommerceTestFixture>
{
    private readonly CommerceTestFixture _fixture;
    private readonly ILogger<CommerceE2ETests> _logger;

    public CommerceE2ETests(CommerceTestFixture fixture)
    {
        _fixture = fixture;
        _logger = _fixture.ServiceProvider.GetRequiredService<ILogger<CommerceE2ETests>>();
    }

    [Fact]
    public async Task CompleteOrderFlow_WithInventoryReservationAndPayment_ShouldSucceed()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
        var pricingService = scope.ServiceProvider.GetRequiredService<IPricingService>();
        var paymentProvider = _fixture.PaymentProviderMock;
        
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        
        // Setup inventory
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "E2E-PRODUCT-001",
            QuantityOnHand = 100,
            QuantityReserved = 0,
            QuantityAvailable = 100,
            ReorderLevel = 10,
            MaxStockLevel = 200,
            Location = "Main Warehouse",
            LastUpdated = DateTime.UtcNow
        };
        
        context.InventoryItems.Add(inventoryItem);
        
        // Setup discount rule
        var discountRule = new DiscountRule
        {
            Id = Guid.NewGuid(),
            Name = "E2E Test Discount",
            Description = "10% off for orders over $50",
            DiscountType = DiscountType.Percentage,
            Value = 10,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            MinimumOrderAmount = 50,
            MaximumDiscountAmount = 20,
            UsageLimit = 100,
            UsageCount = 0,
            ApplicableToAllProducts = true
        };
        
        context.DiscountRules.Add(discountRule);
        await context.SaveChangesAsync();
        
        // Setup payment provider mock
        paymentProvider.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                Success = true,
                TransactionId = "stripe_pi_e2e_test_001",
                Amount = 54.00m, // $60 - 10% discount
                Currency = "USD",
                Status = "succeeded",
                ProcessedAt = DateTime.UtcNow
            });
        
        // Act
        _logger.LogInformation("Starting complete order flow E2E test");
        
        // Step 1: Reserve inventory
        _logger.LogInformation("Step 1: Reserving inventory");
        var reservationResult = await reservationService.ReserveInventoryAsync(
            productId, 3, customerId, TimeSpan.FromHours(1));
        
        reservationResult.Success.Should().BeTrue();
        reservationResult.ReservationId.Should().NotBeEmpty();
        
        // Step 2: Create order
        _logger.LogInformation("Step 2: Creating order");
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "E2E-ORDER-001",
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            SubTotal = 60.00m,
            TotalAmount = 60.00m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = 3,
                    UnitPrice = 20.00m,
                    TotalPrice = 60.00m,
                    Sku = "E2E-PRODUCT-001"
                }
            }
        };
        
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        
        // Step 3: Apply pricing and discounts
        _logger.LogInformation("Step 3: Applying pricing and discounts");
        var pricingResult = await pricingService.ApplyDiscountsAsync(order);
        
        pricingResult.Should().NotBeNull();
        pricingResult.OriginalAmount.Should().Be(60.00m);
        pricingResult.DiscountAmount.Should().Be(6.00m); // 10% of $60
        pricingResult.FinalAmount.Should().Be(54.00m);
        
        // Step 4: Process payment
        _logger.LogInformation("Step 4: Processing payment");
        var paymentRequest = new PaymentRequest
        {
            OrderId = orderId,
            Amount = pricingResult.FinalAmount,
            Currency = "USD",
            PaymentMethodId = "pm_test_card",
            CustomerId = customerId
        };
        
        var paymentResponse = await paymentProvider.Object.ProcessPaymentAsync(paymentRequest);
        
        paymentResponse.Success.Should().BeTrue();
        paymentResponse.TransactionId.Should().NotBeNullOrEmpty();
        
        // Step 5: Create payment record
        _logger.LogInformation("Step 5: Creating payment record");
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = paymentResponse.Amount,
            Currency = paymentResponse.Currency,
            PaymentProvider = "Stripe",
            ExternalTransactionId = paymentResponse.TransactionId,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Payments.Add(payment);
        
        // Step 6: Update order status
        _logger.LogInformation("Step 6: Updating order status");
        order.Status = OrderStatus.Completed;
        order.TotalAmount = pricingResult.FinalAmount;
        
        await context.SaveChangesAsync();
        
        // Step 7: Confirm reservation (convert to actual inventory reduction)
        _logger.LogInformation("Step 7: Confirming reservation");
        var confirmResult = await reservationService.ConfirmReservationAsync(reservationResult.ReservationId);
        
        confirmResult.Should().BeTrue();
        
        // Assert final state
        _logger.LogInformation("Verifying final state");
        
        // Verify inventory was properly reduced
        var finalInventory = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        
        finalInventory.Should().NotBeNull();
        finalInventory!.QuantityOnHand.Should().Be(97); // 100 - 3
        finalInventory.QuantityReserved.Should().Be(0); // Reservation confirmed
        finalInventory.QuantityAvailable.Should().Be(97);
        
        // Verify order is completed
        var finalOrder = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        
        finalOrder.Should().NotBeNull();
        finalOrder!.Status.Should().Be(OrderStatus.Completed);
        finalOrder.TotalAmount.Should().Be(54.00m);
        
        // Verify payment was recorded
        var finalPayment = await context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
        
        finalPayment.Should().NotBeNull();
        finalPayment!.Status.Should().Be(PaymentStatus.Completed);
        finalPayment.Amount.Should().Be(54.00m);
        
        // Verify inventory ledger entries
        var ledgerEntries = await context.InventoryLedgers
            .Where(l => l.InventoryItemId == inventoryItem.Id)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
        
        ledgerEntries.Should().HaveCount(2); // Reserve + Confirm
        ledgerEntries[0].TransactionType.Should().Be(InventoryTransactionType.Reserved);
        ledgerEntries[0].Quantity.Should().Be(-3);
        ledgerEntries[1].TransactionType.Should().Be(InventoryTransactionType.Sale);
        ledgerEntries[1].Quantity.Should().Be(-3);
        
        _logger.LogInformation("Complete order flow E2E test completed successfully");
    }

    [Fact]
    public async Task SubscriptionRenewalFlow_WithPaymentAndInvoicing_ShouldSucceed()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var paymentProvider = _fixture.PaymentProviderMock;
        var notificationService = _fixture.NotificationServiceMock;
        
        var customerId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        
        // Setup customer
        var customer = new Customer
        {
            Id = customerId,
            Email = "e2e.test@example.com",
            FirstName = "E2E",
            LastName = "Test"
        };
        
        // Setup subscription plan
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "E2E Test Plan",
            Description = "Monthly subscription for E2E testing",
            Price = 29.99m,
            Currency = "USD",
            BillingCycle = BillingCycle.Monthly,
            IsActive = true,
            TrialPeriodDays = 0
        };
        
        // Setup subscription
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            NextBillingDate = DateTime.UtcNow.Date,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-30),
            CurrentPeriodEnd = DateTime.UtcNow.Date,
            Customer = customer,
            Plan = plan
        };
        
        context.Customers.Add(customer);
        context.SubscriptionPlans.Add(plan);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        
        // Setup mocks
        paymentProvider.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResponse
            {
                Success = true,
                TransactionId = "stripe_pi_e2e_renewal_001",
                Amount = 29.99m,
                Currency = "USD",
                Status = "succeeded",
                ProcessedAt = DateTime.UtcNow
            });
        
        notificationService.Setup(n => n.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        
        // Act
        _logger.LogInformation("Starting subscription renewal flow E2E test");
        
        var renewalResult = await subscriptionService.RenewSubscriptionAsync(subscription.Id);
        
        // Assert
        renewalResult.Should().BeTrue();
        
        // Verify subscription was updated
        var updatedSubscription = await context.Subscriptions
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscription.Id);
        
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.Active);
        updatedSubscription.NextBillingDate.Should().Be(DateTime.UtcNow.Date.AddDays(30));
        updatedSubscription.CurrentPeriodStart.Should().Be(DateTime.UtcNow.Date);
        updatedSubscription.CurrentPeriodEnd.Should().Be(DateTime.UtcNow.Date.AddDays(30));
        
        // Verify invoice was created
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.SubscriptionId == subscription.Id);
        
        invoice.Should().NotBeNull();
        invoice!.Amount.Should().Be(29.99m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.DueDate.Should().Be(DateTime.UtcNow.Date);
        
        // Verify payment was processed
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.InvoiceId == invoice.Id);
        
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(29.99m);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ExternalTransactionId.Should().Be("stripe_pi_e2e_renewal_001");
        
        // Verify notification was sent
        notificationService.Verify(n => n.SendEmailAsync(
            "e2e.test@example.com",
            It.Is<string>(s => s.Contains("Invoice")),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Once);
        
        _logger.LogInformation("Subscription renewal flow E2E test completed successfully");
    }

    [Fact]
    public async Task PaymentReconciliationFlow_WithGatewayStatement_ShouldMatchTransactions()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        
        // Setup test data
        var orders = new List<Order>();
        var payments = new List<Payment>();
        var transactions = new List<StatementTransaction>();
        
        for (int i = 0; i < 5; i++)
        {
            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var externalTxnId = $"stripe_pi_e2e_recon_{i:D3}";
            var amount = 25.00m + (i * 5);
            
            var order = new Order
            {
                Id = orderId,
                OrderNumber = $"E2E-RECON-{i:D3}",
                CustomerId = Guid.NewGuid(),
                Status = OrderStatus.Completed,
                TotalAmount = amount,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
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
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Order = order
            };
            
            var transaction = new StatementTransaction
            {
                Id = $"e2e_txn_{i:D3}",
                ExternalTransactionId = externalTxnId,
                Amount = amount,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddHours(-2),
                ReferenceNumber = order.OrderNumber,
                TransactionType = "Payment",
                Status = "Completed"
            };
            
            orders.Add(order);
            payments.Add(payment);
            transactions.Add(transaction);
        }
        
        // Add one unmatched transaction
        transactions.Add(new StatementTransaction
        {
            Id = "e2e_txn_unmatched",
            ExternalTransactionId = "stripe_pi_e2e_unmatched",
            Amount = 99.99m,
            Currency = "USD",
            TransactionDate = DateTime.UtcNow.AddHours(-1),
            ReferenceNumber = "UNMATCHED-ORDER",
            TransactionType = "Payment",
            Status = "Completed"
        });
        
        context.Orders.AddRange(orders);
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        
        var statement = new PaymentGatewayStatement
        {
            Id = "e2e_stmt_001",
            PaymentProvider = "Stripe",
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow,
            Transactions = transactions
        };
        
        // Act
        _logger.LogInformation("Starting payment reconciliation flow E2E test");
        
        var reconciliationResult = await reconciliationService.ProcessStatementAsync(statement);
        
        // Assert
        reconciliationResult.Success.Should().BeTrue();
        reconciliationResult.Summary.Should().NotBeNull();
        
        reconciliationResult.Summary.TotalTransactions.Should().Be(6);
        reconciliationResult.Summary.MatchedTransactions.Should().Be(5);
        reconciliationResult.Summary.UnmatchedTransactions.Should().Be(1);
        reconciliationResult.Summary.MatchRate.Should().BeApproximately(83.33m, 0.1m); // 5/6 * 100
        
        // Verify reconciliation session was created
        var session = await context.ReconciliationSessions
            .Include(s => s.MatchedTransactions)
            .Include(s => s.UnmatchedTransactions)
            .FirstOrDefaultAsync(s => s.StatementId == statement.Id);
        
        session.Should().NotBeNull();
        session!.Status.Should().Be(ReconciliationStatus.Completed);
        session.TotalTransactions.Should().Be(6);
        session.MatchedCount.Should().Be(5);
        session.UnmatchedCount.Should().Be(1);
        
        // Verify matched transactions
        session.MatchedTransactions.Should().HaveCount(5);
        foreach (var match in session.MatchedTransactions)
        {
            match.ConfidenceScore.Should().BeGreaterThan(90);
            match.PaymentId.Should().NotBeEmpty();
        }
        
        // Verify unmatched transaction
        session.UnmatchedTransactions.Should().HaveCount(1);
        var unmatchedTxn = session.UnmatchedTransactions.First();
        unmatchedTxn.TransactionId.Should().Be("e2e_txn_unmatched");
        unmatchedTxn.Reason.Should().Contain("No matching payment found");
        
        _logger.LogInformation("Payment reconciliation flow E2E test completed successfully");
    }

    [Fact]
    public async Task RefundFlow_WithInventoryRestoration_ShouldSucceed()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var refundService = scope.ServiceProvider.GetRequiredService<IRefundService>();
        var paymentProvider = _fixture.PaymentProviderMock;
        var walletService = _fixture.WalletServiceMock;
        
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        
        // Setup inventory
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "E2E-REFUND-PRODUCT",
            QuantityOnHand = 95, // Already reduced from previous sale
            QuantityReserved = 0,
            QuantityAvailable = 95,
            ReorderLevel = 10,
            MaxStockLevel = 200,
            Location = "Main Warehouse",
            LastUpdated = DateTime.UtcNow
        };
        
        // Setup completed order
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "E2E-REFUND-ORDER",
            CustomerId = customerId,
            Status = OrderStatus.Completed,
            SubTotal = 75.00m,
            TotalAmount = 75.00m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = 5,
                    UnitPrice = 15.00m,
                    TotalPrice = 75.00m,
                    Sku = "E2E-REFUND-PRODUCT"
                }
            }
        };
        
        // Setup payment
        var payment = new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            Amount = 75.00m,
            Currency = "USD",
            PaymentProvider = "Stripe",
            ExternalTransactionId = "stripe_pi_e2e_refund_original",
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        
        context.InventoryItems.Add(inventoryItem);
        context.Orders.Add(order);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        
        // Setup mocks
        paymentProvider.Setup(p => p.RefundPaymentAsync(
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new RefundResponse
            {
                Success = true,
                RefundId = "stripe_re_e2e_refund_001",
                Amount = 75.00m,
                Currency = "USD",
                Status = "succeeded",
                ProcessedAt = DateTime.UtcNow
            });
        
        walletService.Setup(w => w.CreditWalletAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Act
        _logger.LogInformation("Starting refund flow E2E test");
        
        var refundRequest = new RefundRequest
        {
            OrderId = orderId,
            Amount = 75.00m,
            Reason = "Customer requested refund",
            RefundMethod = RefundMethod.OriginalPayment,
            RequestedBy = "E2E Test"
        };
        
        var refundResult = await refundService.ProcessRefundAsync(refundRequest);
        
        // Assert
        refundResult.Success.Should().BeTrue();
        refundResult.RefundId.Should().NotBeEmpty();
        
        // Verify refund record was created
        var refund = await context.Refunds
            .Include(r => r.Order)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == refundResult.RefundId);
        
        refund.Should().NotBeNull();
        refund!.Amount.Should().Be(75.00m);
        refund.Status.Should().Be(RefundStatus.Completed);
        refund.ExternalRefundId.Should().Be("stripe_re_e2e_refund_001");
        
        // Verify order status was updated
        var updatedOrder = await context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);
        
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Refunded);
        
        // Verify inventory was restored
        var updatedInventory = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        
        updatedInventory.Should().NotBeNull();
        updatedInventory!.QuantityOnHand.Should().Be(100); // 95 + 5 restored
        updatedInventory.QuantityAvailable.Should().Be(100);
        
        // Verify inventory ledger entry for refund
        var refundLedgerEntry = await context.InventoryLedgers
            .Where(l => l.InventoryItemId == inventoryItem.Id)
            .Where(l => l.TransactionType == InventoryTransactionType.Return)
            .FirstOrDefaultAsync();
        
        refundLedgerEntry.Should().NotBeNull();
        refundLedgerEntry!.Quantity.Should().Be(5);
        refundLedgerEntry.ReferenceId.Should().Be(refund.Id.ToString());
        
        // Verify external services were called
        paymentProvider.Verify(p => p.RefundPaymentAsync(
            "stripe_pi_e2e_refund_original", 75.00m, "USD"), Times.Once);
        
        _logger.LogInformation("Refund flow E2E test completed successfully");
    }

    [Fact]
    public async Task ConcurrentInventoryOperations_ShouldPreventOverselling()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        
        var productId = Guid.NewGuid();
        var initialQuantity = 10; // Small quantity to test overselling prevention
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "E2E-CONCURRENT-PRODUCT",
            QuantityOnHand = initialQuantity,
            QuantityReserved = 0,
            QuantityAvailable = initialQuantity,
            ReorderLevel = 2,
            MaxStockLevel = 50,
            Location = "Test Warehouse",
            LastUpdated = DateTime.UtcNow
        };
        
        context.InventoryItems.Add(inventoryItem);
        await context.SaveChangesAsync();
        
        var concurrentUsers = 15; // More users than available inventory
        var reservationQuantity = 2;
        
        // Act
        _logger.LogInformation("Starting concurrent inventory operations E2E test");
        
        var tasks = Enumerable.Range(0, concurrentUsers).Select(async i =>
        {
            using var userScope = _fixture.ServiceProvider.CreateScope();
            var reservationService = userScope.ServiceProvider.GetRequiredService<IReservationService>();
            var customerId = Guid.NewGuid();
            
            try
            {
                return await reservationService.ReserveInventoryAsync(
                    productId, reservationQuantity, customerId, TimeSpan.FromHours(1));
            }
            catch
            {
                return new ReservationResult { Success = false };
            }
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        var successfulReservations = results.Count(r => r.Success);
        var expectedMaxSuccessful = initialQuantity / reservationQuantity; // 10 / 2 = 5
        
        successfulReservations.Should().BeLessOrEqualTo(expectedMaxSuccessful);
        
        // Verify final inventory state
        var finalInventory = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        
        finalInventory.Should().NotBeNull();
        
        var totalReserved = successfulReservations * reservationQuantity;
        finalInventory!.QuantityReserved.Should().Be(totalReserved);
        finalInventory.QuantityAvailable.Should().Be(initialQuantity - totalReserved);
        
        // Ensure no overselling occurred
        (finalInventory.QuantityReserved + finalInventory.QuantityAvailable)
            .Should().Be(initialQuantity);
        
        _logger.LogInformation(
            $"Concurrent inventory test: {successfulReservations}/{concurrentUsers} reservations succeeded");
        _logger.LogInformation("Concurrent inventory operations E2E test completed successfully");
    }

    [Fact]
    public async Task SubscriptionCancellationFlow_WithProration_ShouldSucceed()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var walletService = _fixture.WalletServiceMock;
        
        var customerId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        
        // Setup customer and plan
        var customer = new Customer
        {
            Id = customerId,
            Email = "e2e.cancel@example.com",
            FirstName = "Cancel",
            LastName = "Test"
        };
        
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "E2E Cancellation Test Plan",
            Description = "Monthly plan for cancellation testing",
            Price = 30.00m,
            Currency = "USD",
            BillingCycle = BillingCycle.Monthly,
            IsActive = true,
            TrialPeriodDays = 0
        };
        
        // Setup active subscription (15 days into billing period)
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-45),
            NextBillingDate = DateTime.UtcNow.AddDays(15), // 15 days remaining
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(15),
            Customer = customer,
            Plan = plan
        };
        
        context.Customers.Add(customer);
        context.SubscriptionPlans.Add(plan);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        
        // Setup wallet service mock for prorated refund
        var expectedProration = 15.00m; // Half of monthly fee for remaining 15 days
        walletService.Setup(w => w.CreditWalletAsync(
            customerId, expectedProration, "USD", It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Act
        _logger.LogInformation("Starting subscription cancellation flow E2E test");
        
        var cancellationResult = await subscriptionService.CancelSubscriptionAsync(
            subscription.Id, "Customer requested cancellation", true); // With prorated refund
        
        // Assert
        cancellationResult.Should().BeTrue();
        
        // Verify subscription status
        var cancelledSubscription = await context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscription.Id);
        
        cancelledSubscription.Should().NotBeNull();
        cancelledSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
        cancelledSubscription.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify cancellation record
        var cancellation = await context.SubscriptionCancellations
            .FirstOrDefaultAsync(c => c.SubscriptionId == subscription.Id);
        
        cancellation.Should().NotBeNull();
        cancellation!.Reason.Should().Be("Customer requested cancellation");
        cancellation.ProrationAmount.Should().Be(expectedProration);
        cancellation.EffectiveDate.Should().Be(DateTime.UtcNow.Date);
        
        // Verify wallet credit was processed
        walletService.Verify(w => w.CreditWalletAsync(
            customerId, expectedProration, "USD", 
            It.Is<string>(s => s.Contains("prorated refund"))), Times.Once);
        
        _logger.LogInformation("Subscription cancellation flow E2E test completed successfully");
    }
}