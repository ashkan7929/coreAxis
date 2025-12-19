using CoreAxis.Modules.TaskModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.TaskModule.Infrastructure.Data;

public class TaskDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher;

    public TaskDbContext(DbContextOptions<TaskDbContext> options, IDomainEventDispatcher dispatcher) : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<TaskInstance> TaskInstances { get; set; } = null!;
    public DbSet<TaskActionLog> TaskActionLogs { get; set; } = null!;
    public DbSet<TaskComment> TaskComments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<CoreAxis.SharedKernel.DomainEvents.DomainEvent>();
        
        // Schema
        modelBuilder.HasDefaultSchema("task");

        modelBuilder.Entity<TaskInstance>(entity =>
        {
            entity.ToTable("TaskInstances", "task");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkflowId).IsRequired();
            entity.Property(e => e.StepKey).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.AssigneeType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.AssigneeId).IsRequired().HasMaxLength(128);
            
            entity.HasMany(e => e.ActionLogs)
                .WithOne(e => e.Task)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Comments)
                .WithOne(e => e.Task)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskActionLog>(entity =>
        {
            entity.ToTable("TaskActionLogs", "task");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ActorId).IsRequired().HasMaxLength(128);
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments", "task");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthorId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(2000);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchEvents(cancellationToken);
        return result;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await SaveChangesAsync(CancellationToken.None);
    }

    private async Task DispatchEvents(CancellationToken cancellationToken)
    {
        var entities = ChangeTracker
            .Entries<EntityBase>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        await _dispatcher.DispatchAsync(domainEvents);
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
