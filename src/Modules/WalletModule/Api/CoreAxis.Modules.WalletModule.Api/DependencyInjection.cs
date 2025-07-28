using CoreAxis.Modules.WalletModule.Application;
using CoreAxis.Modules.WalletModule.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.WalletModule.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletModuleApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Application layer
        services.AddWalletModuleApplication();
        
        // Add Infrastructure layer
        services.AddWalletModuleInfrastructure(configuration);
        
        // Add Controllers
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);
        
        return services;
    }
}