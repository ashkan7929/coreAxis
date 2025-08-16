using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;

public class ProductOrderDbContext : DbContext
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    
    public ProductOrderDbContext(DbContextOptions<ProductOrderDbContext> options, IDomainEventDispatcher domainEventDispatcher) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent as it's not an entity but a base class for domain events
        modelBuilder.Ignore<DomainEvent>();

        // Apply configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure schema
        modelBuilder.HasDefaultSchema("productorder");

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

        // Collect domain events before saving
        var entitiesWithEvents = ChangeTracker.Entries<EntityBase>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Save changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        if (domainEvents.Any())
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents);
            
            // Clear domain events after dispatching
            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }
        }

        return result;
    }
}