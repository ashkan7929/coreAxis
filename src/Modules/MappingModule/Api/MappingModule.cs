using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using CoreAxis.Modules.MappingModule.Application;
using CoreAxis.Modules.MappingModule.Infrastructure.Services;
using CoreAxis.SharedKernel.Ports;
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
            ?? "Server=194.62.17.5,1433;Database=CoreAxisDb;User Id=coreaxis_user;Password=j5P9SzzCADjguKuV57lLpjxkm7EKGmyVDCgWoUrtVT3aay1C7C;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

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

        // Add SharedKernel Clients
        services.AddScoped<IMappingClient, MappingClient>();
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