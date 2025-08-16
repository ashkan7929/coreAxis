using CoreAxis.Modules.ApiManager.Application;
using CoreAxis.Modules.ApiManager.Infrastructure;
using CoreAxis.Modules.ApiManager.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.ApiManager;

public static class DependencyInjection
{
    public static IServiceCollection AddApiManagerModule(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add all layers
        services.AddApiManagerInfrastructure(configuration);
        services.AddApiManagerApplication();
        services.AddApiManagerPresentation();

        return services;
    }

    public static IServiceCollection AddApiManagerModuleInMemory(
        this IServiceCollection services)
    {
        // Add all layers with in-memory database for testing
        services.AddApiManagerInfrastructureInMemory();
        services.AddApiManagerApplication();
        services.AddApiManagerPresentation();

        return services;
    }
}