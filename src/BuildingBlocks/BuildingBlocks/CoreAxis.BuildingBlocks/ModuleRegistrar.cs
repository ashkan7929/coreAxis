using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CoreAxis.BuildingBlocks
{
    /// <summary>
    /// Handles the discovery and registration of modules in the CoreAxis platform.
    /// </summary>
    public class ModuleRegistrar : IModuleRegistrar
    {
        private readonly List<IModule> _modules = new List<IModule>();

        /// <summary>
        /// Discovers modules from the specified assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for modules.</param>
        /// <returns>A list of discovered modules.</returns>
        public IEnumerable<IModule> DiscoverModules(IEnumerable<Assembly> assemblies)
        {
            var moduleTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            var modules = new List<IModule>();
            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    var module = (IModule)Activator.CreateInstance(moduleType);
                    modules.Add(module);
                }
                catch (MissingMethodException ex)
                {
                    throw new InvalidOperationException($"Module type '{moduleType.FullName}' must have a parameterless constructor.", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create instance of module type '{moduleType.FullName}'.", ex);
                }
            }

            return modules;
        }

        /// <summary>
        /// Discovers and registers all modules implementing IModule interface.
        /// </summary>
        /// <param name="services">The service collection to register module services with.</param>
        /// <param name="modulesPath">Optional path to look for module assemblies. If not provided, uses the default Modules directory.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection DiscoverAndRegisterModules(IServiceCollection services, string modulesPath = null)
        {
            // Clear any previously registered modules
            _modules.Clear();

            // If no modules path is provided, use the default path
            if (string.IsNullOrEmpty(modulesPath))
            {
                // Get the base directory of the application
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                modulesPath = Path.Combine(baseDir, "Modules");
            }

            // Get all assemblies in the current AppDomain
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Discover modules
            var modules = DiscoverModules(assemblies);

            // Register modules
            RegisterModules(modules, services);

            return services;
        }

        /// <summary>
        /// Registers the specified modules with the service collection.
        /// </summary>
        /// <param name="modules">The modules to register.</param>
        /// <param name="services">The service collection to register with.</param>
        public void RegisterModules(IEnumerable<IModule> modules, IServiceCollection services)
        {
            foreach (var module in modules)
            {
                _modules.Add(module);
                module.RegisterServices(services);
                Console.WriteLine($"Module registered: {module.Name}");
            }
        }

        /// <summary>
        /// Configures the specified modules in the application pipeline.
        /// </summary>
        /// <param name="modules">The modules to configure.</param>
        /// <param name="app">The application builder to configure with.</param>
        public void ConfigureModules(IEnumerable<IModule> modules, IApplicationBuilder app)
        {
            foreach (var module in modules)
            {
                module.ConfigureApplication(app);
                Console.WriteLine($"Module configured: {module.Name}");
            }
        }

        /// <summary>
        /// Configures all discovered modules in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder to configure modules with.</param>
        /// <returns>The application builder for chaining.</returns>
        public IApplicationBuilder ConfigureModules(IApplicationBuilder app)
        {
            ConfigureModules(_modules, app);
            return app;
        }

        /// <summary>
        /// Discovers and registers all modules implementing IModule interface.
        /// </summary>
        /// <param name="services">The service collection to register module services with.</param>
        /// <returns>The discovered modules.</returns>
        public IEnumerable<IModule> DiscoverAndRegisterModules(IServiceCollection services)
        {
            // Call the existing method that takes an optional modulesPath parameter
            DiscoverAndRegisterModules(services, null);
            return _modules;
        }

        /// <summary>
        /// Gets all registered modules.
        /// </summary>
        /// <returns>A list of registered modules.</returns>
        public IReadOnlyList<IModule> GetRegisteredModules()
        {
            return _modules.AsReadOnly();
        }
    }
}