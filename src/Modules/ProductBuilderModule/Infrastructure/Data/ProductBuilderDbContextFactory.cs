using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreAxis.Modules.ProductBuilderModule.Infrastructure.Data;

public class ProductBuilderDbContextFactory : IDesignTimeDbContextFactory<ProductBuilderDbContext>
{
    public ProductBuilderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductBuilderDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COREAXIS_CONNECTION_STRING environment variable is not set.");
        optionsBuilder.UseSqlServer(connectionString);

        return new ProductBuilderDbContext(optionsBuilder.Options);
    }
}
