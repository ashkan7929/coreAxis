using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Interfaces;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using CoreAxis.Modules.CommerceModule.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CoreAxis.Modules.CommerceModule.Tests.Shared;

/// <summary>
/// Base class for Commerce Module tests providing common setup and utilities
/// </summary>
public abstract class CommerceTestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;
    protected readonly CommerceDbContext DbContext;
    protected readonly ServiceProvider ServiceProvider;
    protected readonly Mock<ILogger<CommerceTestBase>> MockLogger;
    
    // Mock services
    protected readonly Mock<IPaymentService> MockPaymentService;
    protected readonly Mock<IWalletService> MockWalletService;
    protected readonly Mock<INotificationService> MockNotificationService;
    protected readonly Mock<IEmailService> MockEmailService;
    protected readonly Mock<IAuditService> MockAuditService;
    
    // Real services (configured with mocks)
    protected readonly IInventoryService InventoryService;
    protected readonly IOrderService OrderService;
    protected readonly IPricingService PricingService;
    protected readonly ISubscriptionService SubscriptionService;
    protected readonly IReconciliationService ReconciliationService;
    protected readonly IRefundService RefundService;
    
    protected CommerceTestBase(ITestOutputHelper output)
    {
        Output = output;
        MockLogger = new Mock<ILogger<CommerceTestBase>>();
        
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        
        DbContext = new CommerceDbContext(options);
        
        // Setup mock services
        SetupMockServices();
        
        // Setup service collection
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        // Get real services
        InventoryService = ServiceProvider.GetRequiredService<IInventoryService>();
        OrderService = ServiceProvider.GetRequiredService<IOrderService>();
        PricingService = ServiceProvider.GetRequiredService<IPricingService>();
        SubscriptionService = ServiceProvider.GetRequiredService<ISubscriptionService>();
        ReconciliationService = ServiceProvider.GetRequiredService<IReconciliationService>();
        RefundService = ServiceProvider.GetRequiredService<IRefundService>();
        
        // Seed test data if needed
        SeedTestData();
    }
    
    #region Setup Methods
    
    private void SetupMockServices()
    {
        MockPaymentService = new Mock<IPaymentService>();
        MockWalletService = new Mock<IWalletService>();
        MockNotificationService = new Mock<INotificationService>();
        MockEmailService = new Mock<IEmailService>();
        MockAuditService = new Mock<IAuditService>();
        
        // Setup default mock behaviors
        SetupDefaultMockBehaviors();
    }
    
    private void SetupDefaultMockBehaviors()
    {
        // Payment Service - Default successful payment
        MockPaymentService
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentRequest request, CancellationToken _) => 
                CommerceTestUtilities.CreateMockPaymentResponse(true, request.Amount, request.Currency));
        
        // Wallet Service - Default successful transactions
        MockWalletService
            .Setup(x => x.DebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid customerId, decimal amount, string description, CancellationToken _) => 
                CommerceTestUtilities.CreateMockWalletTransactionResult(true, amount, 1000m));
        
        MockWalletService
            .Setup(x => x.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid customerId, decimal amount, string description, CancellationToken _) => 
                CommerceTestUtilities.CreateMockWalletTransactionResult(true, amount, 1000m + amount));
        
        MockWalletService
            .Setup(x => x.GetBalanceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000m);
        
        // Notification Service - Always succeeds
        MockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Email Service - Always succeeds
        MockEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Audit Service - Always succeeds
        MockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Register DbContext
        services.AddSingleton(DbContext);
        
        // Register mock services
        services.AddSingleton(MockPaymentService.Object);
        services.AddSingleton(MockWalletService.Object);
        services.AddSingleton(MockNotificationService.Object);
        services.AddSingleton(MockEmailService.Object);
        services.AddSingleton(MockAuditService.Object);
        
        // Register repositories
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        
        // Register real services
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IRefundService, RefundService>();
        
        // Register logging
        services.AddLogging(builder => builder.AddXUnit(Output));
    }
    
    protected virtual void SeedTestData()
    {
        // Override in derived classes to seed specific test data
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Creates and saves a test customer
    /// </summary>
    protected async Task<Customer> CreateTestCustomerAsync(string segment = "Standard")
    {
        var customer = CommerceTestUtilities.GetCustomerFaker().Generate();
        customer.Segment = segment;
        
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();
        
        return customer;
    }
    
    /// <summary>
    /// Creates and saves test inventory items
    /// </summary>
    protected async Task<List<InventoryItem>> CreateTestInventoryAsync(int count = 5, int quantityPerItem = 100)
    {
        var items = CommerceTestUtilities.GetInventoryItemFaker().Generate(count);
        
        foreach (var item in items)
        {
            item.QuantityOnHand = quantityPerItem;
            item.QuantityReserved = 0;
            item.QuantityAvailable = quantityPerItem;
        }
        
        DbContext.InventoryItems.AddRange(items);
        await DbContext.SaveChangesAsync();
        
        return items;
    }
    
    /// <summary>
    /// Creates and saves a test subscription plan
    /// </summary>
    protected async Task<SubscriptionPlan> CreateTestSubscriptionPlanAsync(decimal price = 29.99m, string currency = "USD")
    {
        var plan = CommerceTestUtilities.GetSubscriptionPlanFaker().Generate();
        plan.Price = price;
        plan.Currency = currency;
        plan.IsActive = true;
        
        DbContext.SubscriptionPlans.Add(plan);
        await DbContext.SaveChangesAsync();
        
        return plan;
    }
    
    /// <summary>
    /// Creates and saves a test discount rule
    /// </summary>
    protected async Task<DiscountRule> CreateTestDiscountRuleAsync(
        DiscountType discountType = DiscountType.Percentage, 
        decimal value = 10m)
    {
        var discountRule = CommerceTestUtilities.GetDiscountRuleFaker().Generate();
        discountRule.DiscountType = discountType;
        discountRule.Value = value;
        discountRule.IsActive = true;
        discountRule.ValidFrom = DateTime.UtcNow.AddDays(-1);
        discountRule.ValidTo = DateTime.UtcNow.AddDays(30);
        
        DbContext.DiscountRules.Add(discountRule);
        await DbContext.SaveChangesAsync();
        
        return discountRule;
    }
    
    /// <summary>
    /// Creates and saves a test coupon
    /// </summary>
    protected async Task<Coupon> CreateTestCouponAsync(Guid? discountRuleId = null, string code = null)
    {
        var coupon = CommerceTestUtilities.GetCouponFaker().Generate();
        
        if (discountRuleId.HasValue)
            coupon.DiscountRuleId = discountRuleId.Value;
        
        if (!string.IsNullOrEmpty(code))
            coupon.Code = code;
        
        coupon.IsActive = true;
        coupon.ValidFrom = DateTime.UtcNow.AddDays(-1);
        coupon.ValidTo = DateTime.UtcNow.AddDays(30);
        coupon.UsageCount = 0;
        
        DbContext.Coupons.Add(coupon);
        await DbContext.SaveChangesAsync();
        
        return coupon;
    }
    
    /// <summary>
    /// Creates a complete test order with items
    /// </summary>
    protected async Task<Order> CreateTestOrderAsync(
        Guid? customerId = null, 
        int itemCount = 3, 
        OrderStatus status = OrderStatus.Pending)
    {
        var customer = customerId.HasValue 
            ? await DbContext.Customers.FindAsync(customerId.Value)
            : await CreateTestCustomerAsync();
        
        var order = CommerceTestUtilities.CreateCompleteOrder(itemCount);
        order.CustomerId = customer.Id;
        order.Status = status;
        
        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();
        
        return order;
    }
    
    /// <summary>
    /// Clears all data from the test database
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        DbContext.InventoryLedgers.RemoveRange(DbContext.InventoryLedgers);
        DbContext.InventoryReservations.RemoveRange(DbContext.InventoryReservations);
        DbContext.RefundRequests.RemoveRange(DbContext.RefundRequests);
        DbContext.Payments.RemoveRange(DbContext.Payments);
        DbContext.OrderItems.RemoveRange(DbContext.OrderItems);
        DbContext.Orders.RemoveRange(DbContext.Orders);
        DbContext.CouponRedemptions.RemoveRange(DbContext.CouponRedemptions);
        DbContext.Coupons.RemoveRange(DbContext.Coupons);
        DbContext.DiscountRules.RemoveRange(DbContext.DiscountRules);
        DbContext.SubscriptionInvoices.RemoveRange(DbContext.SubscriptionInvoices);
        DbContext.Subscriptions.RemoveRange(DbContext.Subscriptions);
        DbContext.SubscriptionPlans.RemoveRange(DbContext.SubscriptionPlans);
        DbContext.InventoryItems.RemoveRange(DbContext.InventoryItems);
        DbContext.Customers.RemoveRange(DbContext.Customers);
        
        await DbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// Asserts that two decimal values are equal within a small tolerance
    /// </summary>
    protected static void AssertDecimalEqual(decimal expected, decimal actual, decimal tolerance = 0.01m)
    {
        Assert.True(Math.Abs(expected - actual) <= tolerance, 
            $"Expected {expected}, but got {actual}. Difference: {Math.Abs(expected - actual)}");
    }
    
    /// <summary>
    /// Asserts that a task completes within the specified timeout
    /// </summary>
    protected static async Task AssertCompletesWithinTimeout(Func<Task> action, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Assert.True(false, $"Operation did not complete within {timeout.TotalSeconds} seconds");
        }
    }
    
    /// <summary>
    /// Logs a message to the test output
    /// </summary>
    protected void LogTestMessage(string message)
    {
        Output.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}");
    }
    
    /// <summary>
    /// Logs performance metrics
    /// </summary>
    protected void LogPerformanceMetrics(string operation, TimeSpan duration, int itemCount = 1)
    {
        var throughput = itemCount / duration.TotalSeconds;
        LogTestMessage($"Performance - {operation}: {duration.TotalMilliseconds:F2}ms for {itemCount} items ({throughput:F2} items/sec)");
    }
    
    #endregion
    
    #region Mock Configuration Helpers
    
    /// <summary>
    /// Configures payment service to fail
    /// </summary>
    protected void ConfigurePaymentServiceToFail(string errorMessage = "Payment failed")
    {
        MockPaymentService
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentRequest request, CancellationToken _) => 
                new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = "failed"
                });
    }
    
    /// <summary>
    /// Configures wallet service to have insufficient balance
    /// </summary>
    protected void ConfigureWalletServiceInsufficientBalance(decimal balance = 0m)
    {
        MockWalletService
            .Setup(x => x.GetBalanceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);
        
        MockWalletService
            .Setup(x => x.DebitAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid customerId, decimal amount, string description, CancellationToken _) => 
                new WalletTransactionResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient wallet balance",
                    Amount = amount,
                    Balance = balance
                });
    }
    
    /// <summary>
    /// Configures notification service to fail
    /// </summary>
    protected void ConfigureNotificationServiceToFail()
    {
        MockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Notification service unavailable"));
    }
    
    #endregion
    
    #region Verification Helpers
    
    /// <summary>
    /// Verifies that payment was processed
    /// </summary>
    protected void VerifyPaymentProcessed(decimal amount, string currency = "USD", Times? times = null)
    {
        MockPaymentService.Verify(
            x => x.ProcessPaymentAsync(
                It.Is<PaymentRequest>(r => r.Amount == amount && r.Currency == currency),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once);
    }
    
    /// <summary>
    /// Verifies that wallet was debited
    /// </summary>
    protected void VerifyWalletDebited(Guid customerId, decimal amount, Times? times = null)
    {
        MockWalletService.Verify(
            x => x.DebitAsync(
                customerId,
                amount,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once);
    }
    
    /// <summary>
    /// Verifies that wallet was credited
    /// </summary>
    protected void VerifyWalletCredited(Guid customerId, decimal amount, Times? times = null)
    {
        MockWalletService.Verify(
            x => x.CreditAsync(
                customerId,
                amount,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once);
    }
    
    /// <summary>
    /// Verifies that notification was sent
    /// </summary>
    protected void VerifyNotificationSent(string type, Times? times = null)
    {
        MockNotificationService.Verify(
            x => x.SendNotificationAsync(
                type,
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once);
    }
    
    /// <summary>
    /// Verifies that email was sent
    /// </summary>
    protected void VerifyEmailSent(string recipient, Times? times = null)
    {
        MockEmailService.Verify(
            x => x.SendEmailAsync(
                recipient,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once);
    }
    
    /// <summary>
    /// Verifies that audit event was logged
    /// </summary>
    protected void VerifyAuditLogged(string eventType, Times? times = null)
    {
        MockAuditService.Verify(
            x => x.LogEventAsync(
                eventType,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            times ?? Times.Once);
    }
    
    #endregion
    
    #region Disposal
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServiceProvider?.Dispose();
            DbContext?.Dispose();
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    #endregion
}