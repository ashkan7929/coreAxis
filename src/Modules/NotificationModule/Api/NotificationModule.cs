using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.NotificationModule.Application.Contracts;
using CoreAxis.Modules.NotificationModule.Application.Services;
using CoreAxis.Modules.NotificationModule.Infrastructure.Data;
using CoreAxis.Modules.NotificationModule.Infrastructure.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.NotificationModule.Api;

public class NotificationModule : IModule
{
    public string Name => "Notification Module";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=CoreAxis_Notifications;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });
        });

        services.AddScoped<INotificationProvider, EmailNotificationProvider>();
        services.AddScoped<NotificationService>();

        services.AddControllers()
            .AddApplicationPart(typeof(NotificationModule).Assembly);
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NotificationModule migration skipped due to error: {ex.Message}");
        }

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}
