using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.SharedKernel.Outbox;
using System.Reflection;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data;

public class MLMDbContext : DbContext
{
    public DbSet<UserReferral> UserReferrals { get; set; }
    public DbSet<CommissionRuleSet> CommissionRuleSets { get; set; }
    public DbSet<CommissionRuleVersion> CommissionRuleVersions { get; set; }
    public DbSet<CommissionLevel> CommissionLevels { get; set; }
    public DbSet<CommissionTransaction> CommissionTransactions { get; set; }
    public DbSet<ProductRuleBinding> ProductRuleBindings { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    
    public MLMDbContext(DbContextOptions<MLMDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Ignore DomainEvent as it's not an entity but a base class for domain events
        modelBuilder.Ignore<DomainEvent>();
        
        // Apply all configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Set default schema
        modelBuilder.HasDefaultSchema("mlm");
        
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
            entity.HasIndex(e => e.OccurredOn)
                  .HasDatabaseName("IX_OutboxMessages_OccurredOn");
            entity.HasIndex(e => e.Type)
                  .HasDatabaseName("IX_OutboxMessages_Type");
        });
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle domain events before saving
        var domainEvents = ChangeTracker.Entries<EntityBase>()
            .Select(x => x.Entity)
            .SelectMany(x => x.DomainEvents)
            .ToList();
            
        // Clear domain events to prevent them from being raised again
        foreach (var entity in ChangeTracker.Entries<EntityBase>().Select(x => x.Entity))
        {
            entity.ClearDomainEvents();
        }
        
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Publish domain events after successful save
        // This would be handled by a domain event dispatcher in a real implementation
        
        return result;
    }
}