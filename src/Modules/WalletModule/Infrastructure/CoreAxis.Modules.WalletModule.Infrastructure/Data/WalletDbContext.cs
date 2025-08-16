using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletType> WalletTypes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionType> TransactionTypes { get; set; }
    public DbSet<WalletProvider> WalletProviders { get; set; }
    public DbSet<WalletContract> WalletContracts { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent as it's not an entity but a base class for domain events
        modelBuilder.Ignore<DomainEvent>();

        // Apply configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure schema
        modelBuilder.HasDefaultSchema("wallet");

        // Configure OutboxMessage entity
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.OccurredOn).IsRequired();
            entity.Property(e => e.ProcessedOn);
            entity.Property(e => e.Error).HasMaxLength(1000);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.MaxRetries).HasDefaultValue(3);
            entity.Property(e => e.NextRetryAt);
            entity.Property(e => e.CorrelationId).IsRequired();
            entity.Property(e => e.CausationId);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(100).HasDefaultValue("default");
            
            entity.HasIndex(e => new { e.ProcessedOn, e.NextRetryAt })
                  .HasDatabaseName("IX_OutboxMessages_Processing");
            entity.HasIndex(e => e.CorrelationId)
                  .HasDatabaseName("IX_OutboxMessages_CorrelationId");
        });

        // Configure base entity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.Id))
                    .HasDefaultValueSql("NEWID()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.CreatedOn))
                    .HasDefaultValueSql("GETUTCDATE()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.CreatedBy))
                    .HasMaxLength(256);

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.LastModifiedBy))
                    .HasMaxLength(256);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle audit fields
        var entries = ChangeTracker.Entries<EntityBase>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = DateTime.UtcNow;
                // CreatedBy should be set by the application layer
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedOn = DateTime.UtcNow;
                // LastModifiedBy should be set by the application layer
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}