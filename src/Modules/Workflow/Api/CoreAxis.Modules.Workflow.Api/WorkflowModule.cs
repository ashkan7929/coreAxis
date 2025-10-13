using CoreAxis.BuildingBlocks;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.Modules.Workflow.Infrastructure.EventHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Application.Idempotency;
using CoreAxis.Modules.Workflow.Api.Filters;
using System.Linq;

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

        // Register Workflow DbContext (SQL Server via env var or fallback localdb)
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=CoreAxis_Workflow;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<WorkflowDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "workflow");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });
        });

        // Register admin service
        services.AddScoped<IWorkflowAdminService, WorkflowAdminService>();

        // Register idempotency service
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IdempotencyFilter>();

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

        // Ensure DB is migrated and seed a sample DSL (Alborz) published
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WorkflowModule seeding skipped due to error: {ex.Message}");
        }

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}