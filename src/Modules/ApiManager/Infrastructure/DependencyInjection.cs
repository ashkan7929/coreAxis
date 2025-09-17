using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreAxis.Modules.ApiManager.Application.Abstractions;
using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.ApiManager.Infrastructure.Repositories;
using CoreAxis.Shared.Abstractions.Repositories;

namespace CoreAxis.Modules.ApiManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApiManagerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<ApiManagerDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IWebServiceRepository, WebServiceRepository>();
        services.AddScoped<IWebServiceMethodRepository, WebServiceMethodRepository>();
        services.AddScoped<IWebServiceCallLogRepository, WebServiceCallLogRepository>();
        services.AddScoped<ISecurityProfileRepository, SecurityProfileRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Services
        services.AddScoped<IApiProxy, ApiProxyService>();

        // HTTP Client for external API calls
        services.AddHttpClient<IApiProxy, ApiProxyService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}