using CoreAxis.Modules.FileModule.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CoreAxis.Modules.FileModule.Infrastructure.Data;

public class FileDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public FileDbContext(DbContextOptions<FileDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<FileMetadata> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("files");

        modelBuilder.Ignore<CoreAxis.SharedKernel.DomainEvents.DomainEvent>();

        // Apply tenant filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(FileDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }

        modelBuilder.Entity<FileMetadata>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            b.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            b.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);
            b.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
            
            b.HasIndex(e => e.TenantId);
        });
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder) where T : class, IMustHaveTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
    }
}
