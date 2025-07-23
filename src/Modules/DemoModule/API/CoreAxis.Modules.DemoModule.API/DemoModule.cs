using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.DemoModule.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreAxis.Modules.DemoModule.API
{
    /// <summary>
    /// Demo module implementation of the IModule interface.
    /// </summary>
    public class DemoModule : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name => "DemoModule";

        /// <summary>
        /// Gets the version of the module.
        /// </summary>
        public string Version => "1.0.0";

        /// <summary>
        /// Registers the module services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void RegisterServices(IServiceCollection services)
        {
            // Register domain services
            services.AddScoped<Domain.IDemoItemRepository, Infrastructure.DemoItemRepository>();

            // Register application services
            services.AddScoped<Application.Services.IDemoItemService, Application.Services.DemoItemService>();

            // Register controllers
            services.AddControllers()
                .AddApplicationPart(typeof(DemoModule).Assembly);

            Console.WriteLine($"Module {Name} v{Version} services registered.");
        }

        /// <summary>
        /// Configures the module in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        public void ConfigureApplication(IApplicationBuilder app)
        {
            // Module-specific middleware can be added here
            Console.WriteLine($"Module {Name} v{Version} configured.");
        }
    }
}