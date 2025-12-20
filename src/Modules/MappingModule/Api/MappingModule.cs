using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using CoreAxis.Modules.MappingModule.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreAxis.Modules.MappingModule.Api;

/// <summary>
/// Module definition for the Mapping Module.
/// Registers services and middleware for mapping functionality.
/// </summary>
public class MappingModule : IModule
{
    /// <inheritdoc/>
    public string Name => "Mapping Module";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        // Add Application services
        services.AddMappingModuleApplication();
        
        // DbContext
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=CoreAxis_Mapping;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<MappingDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "mapping");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });
        });

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CoreAxis.Modules.MappingModule.Application.Commands.CreateMappingDefinitionCommand).Assembly));

        // Add Controllers
        services.AddControllers()
            .AddApplicationPart(typeof(MappingModule).Assembly);
    }

    /// <inheritdoc/>
    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Migrate DB
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MappingDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MappingModule migration skipped: {ex.Message}");
        }

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}