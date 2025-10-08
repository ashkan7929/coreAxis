using CoreAxis.BuildingBlocks;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.Modules.Workflow.Infrastructure.EventHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.Workflow.Api;

/// <summary>
/// Workflow Module registers API and subscribes to OrderFinalized to start post-finalize workflows.
/// </summary>
public class WorkflowModule : IModule
{
    public string Name => "Workflow Module";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        // Register handler for DI
        services.AddTransient<OrderFinalizedStartPostFinalizeHandler>();

        // Add controllers from this module
        services.AddControllers()
            .AddApplicationPart(typeof(WorkflowModule).Assembly);
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        // Subscribe to OrderFinalized to start post-finalize workflow
        eventBus.Subscribe<OrderFinalized, OrderFinalizedStartPostFinalizeHandler>();

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}