using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.Workflow.Infrastructure.Data;

public class WorkflowDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ITenantProvider _tenantProvider;

    public WorkflowDbContext(
        DbContextOptions<WorkflowDbContext> options, 
        IDomainEventDispatcher dispatcher,
        ITenantProvider tenantProvider) 
        : base(options)
    {
        _dispatcher = dispatcher;
        _tenantProvider = tenantProvider;
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = null!;
    public DbSet<WorkflowDefinitionVersion> WorkflowDefinitionVersions { get; set; } = null!;
    public DbSet<WorkflowRun> WorkflowRuns { get; set; } = null!;
    public DbSet<WorkflowRunStep> WorkflowRunSteps { get; set; } = null!;
    public DbSet<WorkflowSignal> WorkflowSignals { get; set; } = null!;
    public DbSet<WorkflowTransition> WorkflowTransitions { get; set; } = null!;
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = null!;
    public DbSet<WorkflowTimer> WorkflowTimers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply tenant filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(WorkflowDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
        
        modelBuilder.Ignore<CoreAxis.SharedKernel.DomainEvents.DomainEvent>();

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WorkflowDefinitions", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<WorkflowDefinitionVersion>(entity =>
        {
            entity.ToTable("WorkflowDefinitionVersions", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DslJson).IsRequired();
            entity.Property(e => e.VersionNumber).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.HasOne(v => v.WorkflowDefinition)
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.WorkflowDefinitionId);
            entity.HasIndex(e => new { e.WorkflowDefinitionId, e.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("WorkflowRuns", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkflowDefinitionCode).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ContextJson).IsRequired();
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<WorkflowRunStep>(entity =>
        {
            entity.ToTable("WorkflowRunSteps", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.StepType).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Attempts).HasDefaultValue(0);
            entity.Property(e => e.LogJson);
            entity.HasOne(s => s.WorkflowRun)
                .WithMany(r => r.Steps)
                .HasForeignKey(s => s.WorkflowRunId);
        });

        modelBuilder.Entity<WorkflowSignal>(entity =>
        {
            entity.ToTable("WorkflowSignals", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.PayloadJson);
            entity.HasOne(s => s.WorkflowRun)
                .WithMany()
                .HasForeignKey(s => s.WorkflowRunId);
        });

        modelBuilder.Entity<WorkflowTransition>(entity =>
        {
            entity.ToTable("WorkflowTransitions", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromStepId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ToStepId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Condition);
            entity.Property(e => e.TraceJson);
            entity.HasOne(t => t.WorkflowRun)
                .WithMany()
                .HasForeignKey(t => t.WorkflowRunId);
        });

        modelBuilder.Entity<WorkflowTimer>(entity =>
        {
            entity.ToTable("WorkflowTimers", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.SignalName).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.HasIndex(e => new { e.DueAt, e.Status });
        });

        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("IdempotencyKey", "workflow");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Route).IsRequired().HasMaxLength(256);
            entity.Property(e => e.BodyHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ResponseJson);
            entity.Property(e => e.StatusCode).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => new { e.Route, e.Key, e.BodyHash }).IsUnique();
        });
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder) where T : class, IMustHaveTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var entities = ChangeTracker.Entries<EntityBase>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        if (domainEvents.Any())
            await _dispatcher.DispatchAsync(domainEvents);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await SaveChangesAsync(CancellationToken.None);
    }

    public async Task BeginTransactionAsync()
    {
        if (Database.CurrentTransaction != null) return;
        await Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (Database.CurrentTransaction == null) return;
        try
        {
            await SaveChangesAsync();
            await Database.CurrentTransaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (Database.CurrentTransaction == null) return;
        try
        {
            await Database.CurrentTransaction.RollbackAsync();
        }
        finally
        {
            if (Database.CurrentTransaction != null)
            {
                await Database.CurrentTransaction.DisposeAsync();
            }
        }
    }
}
