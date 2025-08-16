using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Infrastructure.Configurations;
using CoreAxis.SharedKernel.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ApiManager.Infrastructure;

public class ApiManagerDbContext : DbContext
{
    public ApiManagerDbContext(DbContextOptions<ApiManagerDbContext> options) : base(options)
    {
    }

    public DbSet<WebService> WebServices { get; set; } = null!;
    public DbSet<SecurityProfile> SecurityProfiles { get; set; } = null!;
    public DbSet<WebServiceMethod> WebServiceMethods { get; set; } = null!;
    public DbSet<WebServiceParam> WebServiceParams { get; set; } = null!;
    public DbSet<WebServiceCallLog> WebServiceCallLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent base class
        modelBuilder.Ignore<DomainEvent>();

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new WebServiceConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityProfileConfiguration());
        modelBuilder.ApplyConfiguration(new WebServiceMethodConfiguration());
        modelBuilder.ApplyConfiguration(new WebServiceParamConfiguration());
        modelBuilder.ApplyConfiguration(new WebServiceCallLogConfiguration());
    }
}