using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.FileModule.Application.Contracts;
using CoreAxis.Modules.FileModule.Application.Services;
using CoreAxis.Modules.FileModule.Infrastructure.Data;
using CoreAxis.Modules.FileModule.Infrastructure.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.FileModule.Api;

public class FileModule : IModule
{
    public string Name => "File Module";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? "Server=194.62.17.5,1433;Database=CoreAxisDb;User Id=coreaxis_user;Password=j5P9SzzCADjguKuV57lLpjxkm7EKGmyVDCgWoUrtVT3aay1C7C;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        services.AddDbContext<FileDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "files");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });
        });

        // Register File Storage Provider
        // Ideally this should be configurable (S3, Local, etc.)
        var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");
        services.AddSingleton<IFileStorageProvider>(sp => 
            new LocalFileStorageProvider(storagePath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalFileStorageProvider>>()));

        // Register Services
        services.AddScoped<FileService>();

        // Add controllers
        services.AddControllers()
            .AddApplicationPart(typeof(FileModule).Assembly);
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FileModule migration skipped due to error: {ex.Message}");
        }

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}
