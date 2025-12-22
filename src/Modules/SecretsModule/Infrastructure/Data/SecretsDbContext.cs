using CoreAxis.Modules.SecretsModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.SecretsModule.Infrastructure.Data;

public class SecretsDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public SecretsDbContext(DbContextOptions<SecretsDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Secret> Secrets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Ignore<CoreAxis.SharedKernel.DomainEvents.DomainEvent>();
        
        modelBuilder.HasDefaultSchema("secrets");

        // Apply tenant filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SecretsDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }

        modelBuilder.Entity<Secret>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Key).IsRequired().HasMaxLength(100);
            b.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            b.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
        });
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder) where T : class, IMustHaveTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    }
}
