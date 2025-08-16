using CoreAxis.Adapters.Stubs;
using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Adapters;

public static class DependencyInjection
{
    /// <summary>
    /// Registers stub implementations for external services.
    /// This is typically used in development and testing environments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAdapterStubs(this IServiceCollection services)
    {
        // Register stub implementations
        services.AddScoped<IPriceProvider, InMemoryPriceProvider>();
        services.AddScoped<IWorkflowClient, WorkflowClientStub>();
        services.AddScoped<ICommissionEngine, InMemoryCommissionEngine>();
        services.AddScoped<IPaymentGateway, InMemoryPaymentGateway>();
        services.AddScoped<INotificationClient, InMemoryNotificationClient>();
        
        return services;
    }
}