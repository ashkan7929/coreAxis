using CoreAxis.Modules.MLMModule.Application.Extensions;
using CoreAxis.Modules.MLMModule.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.MLMModule.API;

public static class DependencyInjection
{
    public static IServiceCollection AddMLMModuleApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Application layer
        services.AddMLMModuleApplication();
        
        // Add Infrastructure layer
        services.AddMLMModuleInfrastructure(configuration);
        
        // Add Controllers
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);
        
        return services;
    }
}