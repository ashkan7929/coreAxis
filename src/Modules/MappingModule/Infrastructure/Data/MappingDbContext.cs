using CoreAxis.Modules.MappingModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.MappingModule.Infrastructure.Data;

/// <summary>
/// Database context for the Mapping Module.
/// </summary>
public class MappingDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    /// <param name="dispatcher">The domain event dispatcher.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public MappingDbContext(
        DbContextOptions<MappingDbContext> options, 
        IDomainEventDispatcher dispatcher,
        ITenantProvider tenantProvider) 
        : base(options)
    {
        _dispatcher = dispatcher;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Gets or sets the mapping definitions.
    /// </summary>
    public DbSet<MappingDefinition> MappingDefinitions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mapping sets.
    /// </summary>
    public DbSet<MappingSet> MappingSets { get; set; } = null!;

    /// <summary>
    /// Gets or sets the mapping test cases.
    /// </summary>
    public DbSet<MappingTestCase> MappingTestCases { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply tenant filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(MappingDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
        
        modelBuilder.Ignore<CoreAxis.SharedKernel.DomainEvents.DomainEvent>();

        modelBuilder.Entity<MappingDefinition>(entity =>
        {
            entity.ToTable("MappingDefinitions", "mapping");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.TenantId).HasMaxLength(64);
            entity.Property(e => e.RulesJson).IsRequired();
            entity.HasMany(e => e.TestCases)
                .WithOne(t => t.MappingDefinition)
                .HasForeignKey(t => t.MappingDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MappingSet>(entity =>
        {
            entity.ToTable("MappingSets", "mapping");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.TenantId).HasMaxLength(64);
            entity.Property(e => e.ItemsJson).IsRequired();
        });

        modelBuilder.Entity<MappingTestCase>(entity =>
        {
            entity.ToTable("MappingTestCases", "mapping");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InputContextJson).IsRequired();
            entity.Property(e => e.ExpectedOutputJson).IsRequired();
        });
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder) where T : class, IMustHaveTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    }

    /// <summary>
    /// Dispatches domain events and saves changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _dispatcher.DispatchAsync(ChangeTracker.Entries<EntityBase>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList());

        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync()
    {
        await _dispatcher.DispatchAsync(ChangeTracker.Entries<EntityBase>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList());

        return await base.SaveChangesAsync();
    }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        await Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        await Database.CommitTransactionAsync();
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        await Database.RollbackTransactionAsync();
    }
}