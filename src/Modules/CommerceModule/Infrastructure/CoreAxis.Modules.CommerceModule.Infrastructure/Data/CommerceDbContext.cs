using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data;

public class CommerceDbContext : DbContext
{
    private readonly ILogger<CommerceDbContext> _logger;
    private readonly ITenantProvider _tenantProvider;

    public CommerceDbContext(
        DbContextOptions<CommerceDbContext> options, 
        ILogger<CommerceDbContext> logger,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _logger = logger;
        _tenantProvider = tenantProvider;
    }

    // Inventory
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryLedger> InventoryLedgers { get; set; }

    // Orders
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    // Payments
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentIntent> PaymentIntents { get; set; }
    public DbSet<Refund> Refunds { get; set; }

    // Subscriptions
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

    // Reconciliation
    public DbSet<ReconciliationSession> ReconciliationSessions { get; set; }
    public DbSet<ReconciliationEntry> ReconciliationEntries { get; set; }

    // Coupons
    public DbSet<CouponRedemption> CouponRedemptions { get; set; }

    // Discount Rules
    public DbSet<DiscountRule> DiscountRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply tenant filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(CommerceDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommerceDbContext).Assembly);

        // Set default schema
        modelBuilder.HasDefaultSchema("commerce");

        _logger?.LogInformation("CommerceDbContext model created successfully");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This should not happen in production as options should be configured via DI
            _logger?.LogWarning("DbContextOptionsBuilder is not configured");
        }

        // Enable sensitive data logging in development
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder) where T : class, IMustHaveTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            _logger?.LogInformation("Successfully saved {Count} changes to CommerceDbContext", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving changes to CommerceDbContext");
            throw;
        }
    }
}