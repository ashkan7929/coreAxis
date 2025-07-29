using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Extension methods for registering SharedKernel services.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds SharedKernel services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSharedKernel(this IServiceCollection services)
        {
            // Register domain event dispatcher
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            // Register localization services
            services.AddScoped<ILocalizationService, LocalizationService>();

            return services;
        }
    }
}