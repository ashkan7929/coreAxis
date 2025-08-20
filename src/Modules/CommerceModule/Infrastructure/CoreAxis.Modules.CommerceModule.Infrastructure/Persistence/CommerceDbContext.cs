using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Infrastructure.Persistence.Configurations;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Persistence;

/// <summary>
/// Database context for Commerce module.
/// </summary>
public class CommerceDbContext : DbContext
{
    public CommerceDbContext(DbContextOptions<CommerceDbContext> options) : base(options)
    {
    }

    #region DbSets

    /// <summary>
    /// Gets or sets the inventory items.
    /// </summary>
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the inventory reservations.
    /// </summary>
    public DbSet<InventoryReservation> InventoryReservations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the inventory ledger entries.
    /// </summary>
    public DbSet<InventoryLedger> InventoryLedger { get; set; } = null!;

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryReservationConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryLedgerConfiguration());

        // Configure domain events (if using outbox pattern)
        ConfigureDomainEvents(modelBuilder);
    }

    private static void ConfigureDomainEvents(ModelBuilder modelBuilder)
    {
        // Configure entities that inherit from EntityBase to ignore domain events
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (typeof(EntityBase).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Ignore(nameof(EntityBase.DomainEvents));
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set audit fields
        var entries = ChangeTracker.Entries<EntityBase>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedOn = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}