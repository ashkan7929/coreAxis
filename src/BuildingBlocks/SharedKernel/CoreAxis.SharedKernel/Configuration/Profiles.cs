using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CoreAxis.SharedKernel.Ports;

namespace CoreAxis.SharedKernel.Configuration;

public static class Profiles
{
    public const string LocalStub = "local-stub";
    public const string Integration = "integration";
    public const string Production = "production";

    public static IServiceCollection AddProfileBasedServices(this IServiceCollection services, IConfiguration configuration)
    {
        var profile = configuration.GetValue<string>("Profile") ?? LocalStub;
        
        return profile.ToLower() switch
        {
            LocalStub => services.AddLocalStubServices(configuration),
            Integration => services.AddIntegrationServices(configuration),
            Production => services.AddProductionServices(configuration),
            _ => throw new ArgumentException($"Unknown profile: {profile}")
        };
    }

    private static IServiceCollection AddLocalStubServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register stub implementations
        // TODO: These should be registered in the Adapters project to avoid circular dependencies
        // services.AddScoped<IPriceProvider, global::CoreAxis.Adapters.Stubs.InMemoryPriceProvider>();
        // services.AddScoped<IWorkflowClient, global::CoreAxis.Adapters.Stubs.WorkflowClientStub>();
        // services.AddScoped<ICommissionEngine, global::CoreAxis.Adapters.Stubs.InMemoryCommissionEngine>();
        // services.AddScoped<IPaymentGateway, global::CoreAxis.Adapters.Stubs.InMemoryPaymentGateway>();
        // services.AddScoped<INotificationClient, global::CoreAxis.Adapters.Stubs.InMemoryNotificationClient>();
        
        return services;
    }

    private static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register integration/real implementations
        // TODO: Implement real adapters when available
        
        // For now, fallback to stubs
        return services.AddLocalStubServices(configuration);
    }

    private static IServiceCollection AddProductionServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register production implementations
        // TODO: Implement production adapters when available
        
        // For now, fallback to stubs
        return services.AddLocalStubServices(configuration);
    }
}