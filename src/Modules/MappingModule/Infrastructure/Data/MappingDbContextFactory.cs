using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAxis.Modules.MappingModule.Infrastructure.Data;

public class MappingDbContextFactory : IDesignTimeDbContextFactory<MappingDbContext>
{
    public MappingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MappingDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COREAXIS_CONNECTION_STRING environment variable is not set.");
        
        optionsBuilder.UseSqlServer(connectionString);

        return new MappingDbContext(optionsBuilder.Options, new DomainEventDispatcher(null!, NullLogger<DomainEventDispatcher>.Instance), new NoOpTenantProvider());
    }

    private sealed class NoOpTenantProvider : ITenantProvider
    {
        public string? TenantId => "default";
    }
}
