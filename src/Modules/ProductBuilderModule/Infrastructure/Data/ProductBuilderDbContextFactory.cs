using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreAxis.Modules.ProductBuilderModule.Infrastructure.Data;

public class ProductBuilderDbContextFactory : IDesignTimeDbContextFactory<ProductBuilderDbContext>
{
    public ProductBuilderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductBuilderDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=CoreAxis_ProductBuilder;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new ProductBuilderDbContext(optionsBuilder.Options);
    }
}
