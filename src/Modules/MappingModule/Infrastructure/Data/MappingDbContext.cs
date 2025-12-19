using CoreAxis.Modules.MappingModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.MappingModule.Infrastructure.Data;

public class MappingDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher;

    public MappingDbContext(DbContextOptions<MappingDbContext> options, IDomainEventDispatcher dispatcher) : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<MappingDefinition> MappingDefinitions { get; set; } = null!;
    public DbSet<MappingSet> MappingSets { get; set; } = null!;
    public DbSet<MappingTestCase> MappingTestCases { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _dispatcher.DispatchAsync(ChangeTracker.Entries<EntityBase>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList());

        await base.SaveChangesAsync(cancellationToken);
        return true;
    }
}
