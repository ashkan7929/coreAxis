using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.WalletModule.Api;

/// <summary>
/// The Wallet Module provides comprehensive wallet and transaction management functionality.
/// </summary>
public class WalletModule : IModule
{
    /// <summary>
    /// Gets the name of the module.
    /// </summary>
    public string Name => "Wallet Module";

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    public string Version => "1.0.0";

    /// <summary>
    /// Registers the module's services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    public void RegisterServices(IServiceCollection services)
    {
        // Get configuration from the service provider
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        
        // Register all WalletModule services including MediatR, repositories, and other dependencies
        services.AddWalletModuleApi(configuration);
        
        // Add controllers from this module
        services.AddControllers()
            .AddApplicationPart(typeof(WalletModule).Assembly);
            
        Console.WriteLine($"Module {Name} v{Version} services registered.");
    }

    /// <summary>
    /// Configures the module's middleware and endpoints in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure middleware with.</param>
    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Configure any module-specific middleware here if needed
        // For now, the WalletModule doesn't require specific middleware configuration
        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}