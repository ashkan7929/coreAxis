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
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CoreAxis_Mapping;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new MappingDbContext(optionsBuilder.Options, new DomainEventDispatcher(null!, NullLogger<DomainEventDispatcher>.Instance));
    }
}
