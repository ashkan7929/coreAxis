using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CoreAxis.Modules.AuthModule.API
{
    /// <summary>
    /// AuthModule implementation that handles user authentication, authorization, and role-based access control.
    /// </summary>
    public class AuthModule : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name => "AuthModule";

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
            services.AddControllers()
                .AddApplicationPart(typeof(AuthModule).Assembly);
        }

        /// <summary>
        /// Configures the module's middleware and endpoints in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder to configure middleware with.</param>
        public void ConfigureApplication(IApplicationBuilder app)
        {
        }
    }
}