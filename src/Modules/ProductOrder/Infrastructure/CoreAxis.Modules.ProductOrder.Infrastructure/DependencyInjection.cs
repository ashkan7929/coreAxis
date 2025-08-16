using CoreAxis.Modules.ProductOrder.Domain.Services;
using CoreAxis.Modules.ProductOrder.Infrastructure.PriceProviders;
using CoreAxis.Modules.ApiManager.Application;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CoreAxis.Modules.ProductOrder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add ApiManager services
        services.AddApiManagerApplication(configuration);
        services.AddApiManagerInfrastructure(configuration);
        
        // Register price provider
        var priceProviderType = configuration.GetValue<string>("ProductOrder:PriceProvider") ?? "ApiManager";
        
        switch (priceProviderType.ToLowerInvariant())
        {
            case "apimanager":
                services.AddScoped<IPriceProvider, PriceProviderViaApiManager>();
                break;
            
            case "stub":
                services.AddScoped<IPriceProvider, StubPriceProvider>();
                break;
            
            default:
                // Default to ApiManager
                services.AddScoped<IPriceProvider, PriceProviderViaApiManager>();
                break;
        }

        return services;
    }
}