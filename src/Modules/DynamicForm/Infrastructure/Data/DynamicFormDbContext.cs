using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Data;

public class DynamicFormDbContext : DbContext
{
    public DynamicFormDbContext(DbContextOptions<DynamicFormDbContext> options) : base(options)
    {
    }

    public DbSet<Form> Forms { get; set; }
    public DbSet<FormField> FormFields { get; set; }
    public DbSet<FormSubmission> FormSubmissions { get; set; }
    public DbSet<FormulaDefinition> FormulaDefinitions { get; set; }
    public DbSet<FormulaVersion> FormulaVersions { get; set; }
    public DbSet<FormulaEvaluationLog> FormulaEvaluationLogs { get; set; }
    public DbSet<FormVersion> FormVersions { get; set; }
    public DbSet<FormAccessPolicy> FormAccessPolicies { get; set; }
    public DbSet<FormAuditLog> FormAuditLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent as it's not an entity but a base class for domain events
        modelBuilder.Ignore<DomainEvent>();

        // Apply configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure schema
        modelBuilder.HasDefaultSchema("dynamicform");

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
            entity.Property(e => e.UserId);
            entity.Property(e => e.TenantId).HasMaxLength(100);

            entity.HasIndex(e => e.OccurredOn);
            entity.HasIndex(e => e.ProcessedOn);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.TenantId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker.Entries<EntityBase>()
            .Select(x => x.Entity)
            .SelectMany(x => x.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await PublishDomainEventAsync(domainEvent);
        }

        return result;
    }

    private async Task PublishDomainEventAsync(DomainEvent domainEvent)
    {
        // Add domain event to outbox for reliable processing
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().Name,
            Content = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredOn = domainEvent.OccurredOn,
            CorrelationId = domainEvent.CorrelationId ?? Guid.NewGuid().ToString(),
            CausationId = domainEvent.CausationId,
            UserId = domainEvent.UserId,
            TenantId = domainEvent.TenantId
        };

        OutboxMessages.Add(outboxMessage);
        await SaveChangesAsync();
    }
}