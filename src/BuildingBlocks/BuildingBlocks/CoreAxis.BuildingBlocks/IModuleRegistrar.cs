using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace CoreAxis.BuildingBlocks
{
    /// <summary>
    /// Interface for the module registrar, which handles the discovery and registration of modules.
    /// </summary>
    public interface IModuleRegistrar
    {
        /// <summary>
        /// Discovers modules from the specified assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for modules.</param>
        /// <returns>A list of discovered modules.</returns>
        IEnumerable<IModule> DiscoverModules(IEnumerable<System.Reflection.Assembly> assemblies);

        /// <summary>
        /// Discovers and registers all modules implementing IModule interface.
        /// </summary>
        /// <param name="services">The service collection to register module services with.</param>
        /// <returns>The discovered modules.</returns>
        IEnumerable<IModule> DiscoverAndRegisterModules(IServiceCollection services);

        /// <summary>
        /// Registers the specified modules with the service collection.
        /// </summary>
        /// <param name="modules">The modules to register.</param>
        /// <param name="services">The service collection to register with.</param>
        void RegisterModules(IEnumerable<IModule> modules, IServiceCollection services);

        /// <summary>
        /// Configures the specified modules in the application pipeline.
        /// </summary>
        /// <param name="modules">The modules to configure.</param>
        /// <param name="app">The application builder to configure with.</param>
        void ConfigureModules(IEnumerable<IModule> modules, IApplicationBuilder app);

        /// <summary>
        /// Gets all registered modules.
        /// </summary>
        /// <returns>A list of registered modules.</returns>
        IReadOnlyList<IModule> GetRegisteredModules();
    }
}