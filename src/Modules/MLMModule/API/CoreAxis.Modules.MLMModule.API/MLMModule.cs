using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.MLMModule.API
{
    /// <summary>
    /// MLMModule implementation that handles multi-level marketing operations including user referrals, commission transactions, and commission rules.
    /// </summary>
    public class MLMModule : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name => "MLMModule";

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
            // Add controllers from this module
            services.AddControllers()
                .AddApplicationPart(typeof(MLMModule).Assembly);
        }

        /// <summary>
        /// Configures the module's middleware and endpoints in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder to configure middleware with.</param>
        public void ConfigureApplication(IApplicationBuilder app)
        {
            // Configure any module-specific middleware here if needed
            // For now, the MLMModule doesn't require specific middleware configuration
        }
    }
}