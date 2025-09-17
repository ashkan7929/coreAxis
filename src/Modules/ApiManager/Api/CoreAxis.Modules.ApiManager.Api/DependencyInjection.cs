using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreAxis.Modules.ApiManager.Application;
using CoreAxis.Modules.ApiManager.Infrastructure;
using CoreAxis.Modules.ApiManager.Presentation;

namespace CoreAxis.Modules.ApiManager.API;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all ApiManager module services including Application, Infrastructure, and Presentation layers
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApiManagerModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Application layer services (MediatR, FluentValidation, Behaviors)
        services.AddApiManagerApplication();
        
        // Register Infrastructure layer services (DbContext, Repositories, UnitOfWork, External Services)
        services.AddApiManagerInfrastructure(configuration);
        
        // Register Presentation layer services (Controllers, API documentation)
        services.AddApiManagerPresentation();
        
        return services;
    }
}