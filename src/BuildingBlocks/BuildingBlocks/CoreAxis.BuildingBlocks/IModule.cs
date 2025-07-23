using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.BuildingBlocks
{
    /// <summary>
    /// Interface for modular components in the CoreAxis platform.
    /// Each module must implement this interface to be discovered and registered.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Registers the module's services with the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        void RegisterServices(IServiceCollection services);

        /// <summary>
        /// Configures the module's middleware and endpoints in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder to configure middleware with.</param>
        void ConfigureApplication(IApplicationBuilder app);
    }
}