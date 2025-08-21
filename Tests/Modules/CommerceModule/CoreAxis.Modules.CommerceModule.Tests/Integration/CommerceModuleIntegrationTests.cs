using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;

namespace CoreAxis.Modules.CommerceModule.Tests.Integration;

public class CommerceModuleIntegrationTests : IClassFixture<CommerceTestFixture>
{
    private readonly CommerceTestFixture _fixture;
    private readonly IServiceScope _scope;
    private readonly CommerceDbContext _context;
    private readonly IReservationService _reservationService;
    private readonly IPricingService _pricingService;
    private readonly IReconciliationService _reconciliationService;

    public CommerceModuleIntegrationTests(CommerceTestFixture fixture)
    {
        _fixture = fixture;
        _scope = _fixture.ServiceProvider.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        _reservationService = _scope.ServiceProvider.GetRequiredService<IReservationService>();
        _pricingService = _scope.ServiceProvider.GetRequiredService<IPricingService>();
        _reconciliationService = _scope.ServiceProvider.GetRequiredService<IReconciliationService>();
    }

    [Fact]
    public async Task CompleteOrderFlow_WithInventoryReservationAndPricing_ShouldWorkEndToEnd()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var quantity = 2;
        var unitPrice = 50.00m;
        var discountPercentage = 10;
        
        // Create test data
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "TEST-SKU-001",
            QuantityOnHand = 10,
            QuantityReserved = 0,
            QuantityAvailable = 10,
            ReorderLevel = 5,
            MaxStockLevel = 100,
            Location = "Warehouse-A",
            LastUpdated = DateTime.UtcNow
        };

        var discountRule = new DiscountRule
        {
            Id = Guid.NewGuid(),
            Name = "10% Off Test",
            Description = "Test discount",
            DiscountType = DiscountType.Percentage,
            Value = discountPercentage,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            MinimumOrderAmount = 0,
            MaximumDiscountAmount = 1000,
            UsageLimit = 100,
            UsageCount = 0,
            ApplicableToAllProducts = true
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-TEST-001",
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            SubTotal = unitPrice * quantity,
            TotalAmount = unitPrice * quantity,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = unitPrice * quantity,
                    Sku = "TEST-SKU-001"
                }
            }
        };

        _context.InventoryItems.Add(inventoryItem);
        _context.DiscountRules.Add(discountRule);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert
        
        // Step 1: Reserve inventory
        var reservationResult = await _reservationService.ReserveInventoryAsync(
            productId, quantity, customerId, TimeSpan.FromHours(1));
        
        reservationResult.Success.Should().BeTrue();
        reservationResult.ReservationId.Should().NotBeEmpty();

        // Verify inventory is reserved
        var updatedInventory = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        updatedInventory!.QuantityReserved.Should().Be(quantity);
        updatedInventory.QuantityAvailable.Should().Be(8); // 10 - 2

        // Step 2: Apply pricing and discounts
        var pricingResult = await _pricingService.ApplyDiscountsAsync(order);
        
        pricingResult.Should().NotBeNull();
        pricingResult.OriginalAmount.Should().Be(100.00m); // 2 * 50
        pricingResult.DiscountAmount.Should().Be(10.00m); // 10% of 100
        pricingResult.FinalAmount.Should().Be(90.00m); // 100 - 10

        // Step 3: Confirm reservation (simulate successful payment)
        var confirmResult = await _reservationService.ConfirmReservationAsync(
            reservationResult.ReservationId, orderId);
        
        confirmResult.Should().BeTrue();

        // Verify final inventory state
        var finalInventory = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        finalInventory!.QuantityOnHand.Should().Be(8); // 10 - 2
        finalInventory.QuantityReserved.Should().Be(0); // Reservation confirmed
        finalInventory.QuantityAvailable.Should().Be(8);

        // Verify reservation is marked as confirmed
        var reservation = await _context.InventoryReservations
            .FirstOrDefaultAsync(r => r.Id == reservationResult.ReservationId);
        reservation!.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task SubscriptionRenewalFlow_WithPaymentAndInvoicing_ShouldWorkEndToEnd()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        
        var customer = new Customer
        {
            Id = customerId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Monthly Premium",
            Description = "Premium monthly subscription",
            Price = 29.99m,
            Currency = "USD",
            BillingCycle = BillingCycle.Monthly,
            IsActive = true,
            TrialPeriodDays = 7,
            Features = new List<string> { "Feature1", "Feature2" }
        };

        var subscription = new Subscription
        {
            Id = subscriptionId,
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

        _context.Customers.Add(customer);
        _context.SubscriptionPlans.Add(plan);
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var subscriptionService = _scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var renewalResult = await subscriptionService.RenewSubscriptionAsync(subscriptionId);

        // Assert
        renewalResult.Should().BeTrue();

        // Verify subscription is updated
        var updatedSubscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
        
        updatedSubscription!.NextBillingDate.Should().BeAfter(DateTime.UtcNow.Date);
        updatedSubscription.CurrentPeriodStart.Should().Be(DateTime.UtcNow.Date);
        updatedSubscription.CurrentPeriodEnd.Should().BeAfter(DateTime.UtcNow.Date);

        // Verify invoice is created
        var invoice = await _context.SubscriptionInvoices
            .FirstOrDefaultAsync(i => i.SubscriptionId == subscriptionId);
        
        invoice.Should().NotBeNull();
        invoice!.Amount.Should().Be(plan.Price);
        invoice.Currency.Should().Be(plan.Currency);
        invoice.Status.Should().Be(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task PaymentReconciliation_WithGatewayStatement_ShouldMatchTransactions()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var externalTransactionId = "stripe_pi_123456";
        var amount = 99.99m;
        var currency = "USD";
        
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-REC-001",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Completed,
            TotalAmount = amount,
            Currency = currency,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var payment = new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            Amount = amount,
            Currency = currency,
            PaymentProvider = "Stripe",
            ExternalTransactionId = externalTransactionId,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Order = order
        };

        _context.Orders.Add(order);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var gatewayStatement = new PaymentGatewayStatement
        {
            Id = "stmt_001",
            PaymentProvider = "Stripe",
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow,
            Transactions = new List<StatementTransaction>
            {
                new StatementTransaction
                {
                    Id = "txn_001",
                    ExternalTransactionId = externalTransactionId,
                    Amount = amount,
                    Currency = currency,
                    TransactionDate = DateTime.UtcNow.AddHours(-2),
                    ReferenceNumber = order.OrderNumber,
                    TransactionType = "Payment",
                    Status = "Completed"
                }
            }
        };

        // Act
        var reconciliationResult = await _reconciliationService.ProcessStatementAsync(gatewayStatement);

        // Assert
        reconciliationResult.Success.Should().BeTrue();
        reconciliationResult.Summary.TotalTransactions.Should().Be(1);
        reconciliationResult.Summary.MatchedTransactions.Should().Be(1);
        reconciliationResult.Summary.UnmatchedTransactions.Should().Be(0);
        reconciliationResult.Summary.MatchRate.Should().Be(100);

        // Verify reconciliation session is created
        var session = await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.PaymentProvider == "Stripe");
        
        session.Should().NotBeNull();
        session!.Status.Should().Be(ReconciliationStatus.Completed);

        // Verify reconciliation entry is created
        var entry = await _context.ReconciliationEntries
            .FirstOrDefaultAsync(e => e.SessionId == session.Id);
        
        entry.Should().NotBeNull();
        entry!.MatchStatus.Should().Be(MatchStatus.Matched);
        entry.PaymentId.Should().Be(paymentId);
        entry.MatchConfidence.Should().BeGreaterThan(90);
    }

    [Fact]
    public async Task RefundFlow_WithInventoryRestoration_ShouldWorkEndToEnd()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var quantity = 3;
        var unitPrice = 25.00m;
        var totalAmount = quantity * unitPrice;
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "REFUND-SKU-001",
            QuantityOnHand = 7, // Already reduced after order
            QuantityReserved = 0,
            QuantityAvailable = 7,
            ReorderLevel = 5,
            MaxStockLevel = 100,
            Location = "Warehouse-B",
            LastUpdated = DateTime.UtcNow.AddHours(-1)
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-REFUND-001",
            CustomerId = customerId,
            Status = OrderStatus.Completed,
            SubTotal = totalAmount,
            TotalAmount = totalAmount,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalAmount,
                    Sku = "REFUND-SKU-001"
                }
            }
        };

        var payment = new Payment
        {
            Id = paymentId,
            OrderId = orderId,
            Amount = totalAmount,
            Currency = "USD",
            PaymentProvider = "Stripe",
            ExternalTransactionId = "pi_refund_test",
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Order = order
        };

        _context.InventoryItems.Add(inventoryItem);
        _context.Orders.Add(order);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act
        var refundRequest = new RefundRequest
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            OrderId = orderId,
            Amount = totalAmount,
            Currency = "USD",
            Reason = "Customer requested refund",
            RequestedBy = customerId,
            Status = RefundStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefundRequests.Add(refundRequest);
        await _context.SaveChangesAsync();

        // Process refund
        var refundService = _scope.ServiceProvider.GetRequiredService<IRefundService>();
        var refundResult = await refundService.ProcessRefundAsync(refundRequest.Id);

        // Assert
        refundResult.Success.Should().BeTrue();

        // Verify refund status
        var updatedRefund = await _context.RefundRequests
            .FirstOrDefaultAsync(r => r.Id == refundRequest.Id);
        updatedRefund!.Status.Should().Be(RefundStatus.Completed);

        // Verify inventory is restored
        var restoredInventory = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        restoredInventory!.QuantityOnHand.Should().Be(10); // 7 + 3 restored
        restoredInventory.QuantityAvailable.Should().Be(10);

        // Verify inventory ledger entry
        var ledgerEntry = await _context.InventoryLedgers
            .Where(l => l.InventoryItemId == inventoryItem.Id)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync();
        
        ledgerEntry.Should().NotBeNull();
        ledgerEntry!.TransactionType.Should().Be(InventoryTransactionType.Refund);
        ledgerEntry.Quantity.Should().Be(quantity);
        ledgerEntry.ReferenceId.Should().Be(refundRequest.Id.ToString());
    }

    [Fact]
    public async Task ConcurrentInventoryReservation_ShouldPreventOverselling()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var availableQuantity = 5;
        var requestedQuantity = 3;
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = "CONCURRENT-SKU-001",
            QuantityOnHand = availableQuantity,
            QuantityReserved = 0,
            QuantityAvailable = availableQuantity,
            ReorderLevel = 2,
            MaxStockLevel = 50,
            Location = "Warehouse-C",
            LastUpdated = DateTime.UtcNow
        };

        _context.InventoryItems.Add(inventoryItem);
        await _context.SaveChangesAsync();

        // Act - Simulate concurrent reservation requests
        var tasks = new List<Task<ReservationResult>>();
        for (int i = 0; i < 3; i++)
        {
            var customerId = Guid.NewGuid();
            var task = Task.Run(async () =>
            {
                using var scope = _fixture.ServiceProvider.CreateScope();
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                return await reservationService.ReserveInventoryAsync(
                    productId, requestedQuantity, customerId, TimeSpan.FromHours(1));
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulReservations = results.Count(r => r.Success);
        var failedReservations = results.Count(r => !r.Success);
        
        // Only one reservation should succeed (5 available, 3 requested, can't fulfill 2 more)
        successfulReservations.Should().Be(1);
        failedReservations.Should().Be(2);

        // Verify final inventory state
        var finalInventory = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId);
        finalInventory!.QuantityReserved.Should().Be(requestedQuantity);
        finalInventory.QuantityAvailable.Should().Be(availableQuantity - requestedQuantity);
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}