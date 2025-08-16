using CoreAxis.Modules.ProductOrder.Application;
using CoreAxis.Modules.ProductOrder.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.ProductOrder;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add all layers
        services.AddProductOrderApplication();
        services.AddProductOrderInfrastructure(configuration);

        return services;
    }
}