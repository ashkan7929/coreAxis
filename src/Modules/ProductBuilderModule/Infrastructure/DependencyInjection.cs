using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.Modules.ProductBuilderModule.Infrastructure.Data;
using CoreAxis.Modules.ProductBuilderModule.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.ProductBuilderModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductBuilderModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProductBuilderDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}
