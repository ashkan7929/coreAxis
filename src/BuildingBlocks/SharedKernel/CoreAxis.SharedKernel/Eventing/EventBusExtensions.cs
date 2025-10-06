using CoreAxis.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CoreAxis.SharedKernel.Eventing;

/// <summary>
/// DI helpers for the CoreAxis EventBus.
/// Default implementation uses InMemoryEventBus suitable for development.
/// To switch to a production transport, replace the registration of IEventBus
/// with your implementation (e.g., RabbitMQEventBus, KafkaEventBus) in this extension.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Registers the CoreAxis EventBus and scans/loads integration event handlers.
    /// </summary>
    public static IServiceCollection AddCoreAxisEventBus(this IServiceCollection services)
    {
        // Register default EventBus implementation for dev
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // Scan currently loaded assemblies for integration event handlers and register them
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            RegisterIntegrationEventHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterIntegrationEventHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IIntegrationEventHandler<>);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var interfaces = type.GetInterfaces()
                                  .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                                  .ToList();

            if (interfaces.Count > 0)
            {
                services.AddTransient(type);
            }
        }
    }
}