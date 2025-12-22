using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.TaskModule.Application;
using CoreAxis.Modules.TaskModule.Infrastructure.Data;
using CoreAxis.Modules.TaskModule.Infrastructure.EventHandlers;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.TaskModule.Api;

public class TaskModule : IModule
{
    public string Name => "Task Module";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        // DbContext
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? "Server=194.62.17.5,1433;Database=CoreAxisDb;User Id=coreaxis_user;Password=j5P9SzzCADjguKuV57lLpjxkm7EKGmyVDCgWoUrtVT3aay1C7C;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        services.AddDbContext<TaskDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "task");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });
        });

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TaskModuleApplication).Assembly));

        // Register Integration Event Handlers
        services.AddTransient<HumanTaskRequestedIntegrationEventHandler>();

        // Add Controllers
        services.AddControllers()
            .AddApplicationPart(typeof(TaskModule).Assembly);
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Subscribe to events
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
        eventBus.Subscribe<HumanTaskRequested, HumanTaskRequestedIntegrationEventHandler>();

        // Migrate DB
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TaskModule migration skipped: {ex.Message}");
        }

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}
