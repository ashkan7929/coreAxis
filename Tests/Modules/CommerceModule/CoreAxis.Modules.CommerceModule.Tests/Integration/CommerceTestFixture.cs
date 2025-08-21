using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using CoreAxis.Shared.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CoreAxis.Modules.CommerceModule.Tests.Integration;

public class CommerceTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    private readonly ServiceCollection _services;
    private CommerceDbContext? _context;

    public CommerceTestFixture()
    {
        _services = new ServiceCollection();
        ConfigureServices();
        ServiceProvider = _services.BuildServiceProvider();
        InitializeDatabase();
    }

    private void ConfigureServices()
    {
        // Add Entity Framework with In-Memory Database
        _services.AddDbContext<CommerceDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Register CommerceDbContext as ICommerceDbContext
        _services.AddScoped<ICommerceDbContext>(provider => 
            provider.GetRequiredService<CommerceDbContext>());

        // Add Logging
        _services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Mock Domain Event Dispatcher
        var mockEventDispatcher = new Mock<IDomainEventDispatcher>();
        _services.AddSingleton(mockEventDispatcher.Object);

        // Register Application Services
        _services.AddScoped<IReservationService, ReservationService>();
        _services.AddScoped<IPricingService, PricingService>();
        _services.AddScoped<IReconciliationService, ReconciliationService>();
        _services.AddScoped<ISubscriptionService, SubscriptionService>();
        _services.AddScoped<IRefundService, RefundService>();

        // Configure Options
        var subscriptionOptions = new SubscriptionSchedulerOptions
        {
            ProcessingIntervalMinutes = 60,
            BatchSize = 100,
            MaxRetryAttempts = 3,
            RetryDelayMinutes = 30,
            GracePeriodDays = 7,
            CleanupRetentionDays = 90
        };
        _services.Configure<SubscriptionSchedulerOptions>(options =>
        {
            options.ProcessingIntervalMinutes = subscriptionOptions.ProcessingIntervalMinutes;
            options.BatchSize = subscriptionOptions.BatchSize;
            options.MaxRetryAttempts = subscriptionOptions.MaxRetryAttempts;
            options.RetryDelayMinutes = subscriptionOptions.RetryDelayMinutes;
            options.GracePeriodDays = subscriptionOptions.GracePeriodDays;
            options.CleanupRetentionDays = subscriptionOptions.CleanupRetentionDays;
        });

        // Add Background Services
        _services.AddSingleton<IHostedService, SubscriptionSchedulerService>();

        // Mock External Dependencies
        var mockPaymentProvider = new Mock<IPaymentProvider>();
        mockPaymentProvider.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = true,
                TransactionId = "mock_txn_" + Guid.NewGuid().ToString("N")[..8],
                Amount = 0, // Will be set by the calling code
                Currency = "USD",
                Status = "Completed"
            });
        _services.AddSingleton(mockPaymentProvider.Object);

        var mockWalletService = new Mock<IWalletService>();
        mockWalletService.Setup(w => w.ProcessRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletTransactionResult
            {
                Success = true,
                TransactionId = "wallet_txn_" + Guid.NewGuid().ToString("N")[..8],
                Amount = 0, // Will be set by the calling code
                Currency = "USD"
            });
        _services.AddSingleton(mockWalletService.Object);

        var mockNotificationService = new Mock<INotificationService>();
        _services.AddSingleton(mockNotificationService.Object);
    }

    private void InitializeDatabase()
    {
        using var scope = ServiceProvider.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        _context.Database.EnsureCreated();
    }

    public async Task CleanupDatabaseAsync()
    {
        if (_context != null)
        {
            // Clear all entities
            _context.InventoryItems.RemoveRange(_context.InventoryItems);
            _context.InventoryReservations.RemoveRange(_context.InventoryReservations);
            _context.InventoryLedgers.RemoveRange(_context.InventoryLedgers);
            _context.DiscountRules.RemoveRange(_context.DiscountRules);
            _context.CouponRedemptions.RemoveRange(_context.CouponRedemptions);
            _context.Orders.RemoveRange(_context.Orders);
            _context.OrderItems.RemoveRange(_context.OrderItems);
            _context.Payments.RemoveRange(_context.Payments);
            _context.RefundRequests.RemoveRange(_context.RefundRequests);
            _context.Customers.RemoveRange(_context.Customers);
            _context.SubscriptionPlans.RemoveRange(_context.SubscriptionPlans);
            _context.Subscriptions.RemoveRange(_context.Subscriptions);
            _context.SubscriptionInvoices.RemoveRange(_context.SubscriptionInvoices);
            _context.ReconciliationSessions.RemoveRange(_context.ReconciliationSessions);
            _context.ReconciliationEntries.RemoveRange(_context.ReconciliationEntries);
            _context.SubscriptionProcessingLogs.RemoveRange(_context.SubscriptionProcessingLogs);
            
            await _context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}

// Mock interfaces and classes for testing
public interface IPaymentProvider
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IWalletService
{
    Task<WalletTransactionResult> ProcessRefundAsync(RefundRequest request, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendNotificationAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default);
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class WalletTransactionResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}