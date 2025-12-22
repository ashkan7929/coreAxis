using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.SecretsModule.Application.Contracts;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.Modules.SecretsModule.Application.Services;
using CoreAxis.Modules.SecretsModule.Infrastructure.Data;
using CoreAxis.Modules.SecretsModule.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.SecretsModule.Api;

public class SecretsModule : IModule
{
    public string Name => "Secrets Module";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? "Server=194.62.17.5,1433;Database=CoreAxisDb;User Id=coreaxis_user;Password=j5P9SzzCADjguKuV57lLpjxkm7EKGmyVDCgWoUrtVT3aay1C7C;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        services.AddDbContext<SecretsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "secrets");
                sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
            });
        });

        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<SecretService>();
        services.AddScoped<ISecretResolver>(sp => sp.GetRequiredService<SecretService>());

        services.AddControllers()
            .AddApplicationPart(typeof(SecretsModule).Assembly);
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecretsDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SecretsModule migration skipped due to error: {ex.Message}");
        }

        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}
