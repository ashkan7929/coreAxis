using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CoreAxis.Modules.ApiManager.API;

public class ApiManagerModule : IModule
{
    public string Name => "ApiManager";
    public string Version => "1.0.0";

    /// <summary>
    /// Registers the module's services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    public void RegisterServices(IServiceCollection services)
    {
        // Get configuration from the service provider
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        // Register all ApiManager services
        services.AddApiManagerModule(configuration);
        
        // Add controllers from this module
        services.AddControllers()
            .AddApplicationPart(typeof(ApiManagerModule).Assembly);
            
        Console.WriteLine($"Module {Name} v{Version} services registered.");
    }

    /// <summary>
    /// Configures the module's middleware and endpoints in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure middleware with.</param>
    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Configure any module-specific middleware here if needed
        // For now, the ApiManager doesn't require specific middleware configuration
        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}