using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain.Entities;
using CoreAxis.Modules.ApiManager.Infrastructure.Configurations;

namespace CoreAxis.Modules.ApiManager.Infrastructure;

public class ApiManagerDbContext : DbContext
{
    public ApiManagerDbContext(DbContextOptions<ApiManagerDbContext> options) : base(options)
    {
    }

    public DbSet<WebService> WebServices { get; set; } = null!;
    public DbSet<WebServiceMethod> WebServiceMethods { get; set; } = null!;
    public DbSet<WebServiceParam> WebServiceParams { get; set; } = null!;
    public DbSet<WebServiceCallLog> WebServiceCallLogs { get; set; } = null!;
    public DbSet<SecurityProfile> SecurityProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new WebServiceConfiguration());
        modelBuilder.ApplyConfiguration(new WebServiceMethodConfiguration());
        modelBuilder.ApplyConfiguration(new WebServiceParamConfiguration());
        modelBuilder.ApplyConfiguration(new WebServiceCallLogConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityProfileConfiguration());

        // Set default schema
        modelBuilder.HasDefaultSchema("ApiManager");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add audit fields before saving
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}