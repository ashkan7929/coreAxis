using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.Modules.ApiManager.Infrastructure.Repositories;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApiManagerInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApiManagerDbContext>(options =>
        {
            options.UseSqlServer(connectionString, b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory", "ApiManager");
                b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            });
            
            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register DbContext as the generic DbContext for dependency injection
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApiManagerDbContext>());

        // Register Repositories
        services.AddScoped<IWebServiceRepository, WebServiceRepository>();
        services.AddScoped<IWebServiceMethodRepository, WebServiceMethodRepository>();
        services.AddScoped<IWebServiceCallLogRepository, WebServiceCallLogRepository>();
        services.AddScoped<ISecurityProfileRepository, SecurityProfileRepository>();

        return services;
    }

    public static IServiceCollection AddApiManagerInfrastructureInMemory(
        this IServiceCollection services)
    {
        // Add In-Memory DbContext for testing
        services.AddDbContext<ApiManagerDbContext>(options =>
        {
            options.UseInMemoryDatabase("ApiManagerTestDb");
            options.EnableSensitiveDataLogging();
        });

        // Register DbContext as the generic DbContext for dependency injection
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApiManagerDbContext>());

        return services;
    }
}