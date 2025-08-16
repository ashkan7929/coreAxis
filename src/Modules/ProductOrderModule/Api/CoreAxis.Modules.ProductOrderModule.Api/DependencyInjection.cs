using CoreAxis.Modules.ProductOrderModule.Application;
using CoreAxis.Modules.ProductOrderModule.Infrastructure;
using CoreAxis.Modules.AuthModule.API.Authz;
using CoreAxis.Adapters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CoreAxis.Modules.ProductOrderModule.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderModuleApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Application layer
        services.AddProductOrderModuleApplication();
        
        // Add Infrastructure layer
        services.AddProductOrderModuleInfrastructure(configuration);
        
        // Add Adapter stubs for external services (WorkflowClient, etc.)
        services.AddAdapterStubs();
        
        // JWT Authentication is configured globally in API Gateway
        // No need to configure it here to avoid "Scheme already exists" error
        
        // Add Authorization with permission policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        
        // Add Controllers
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);
        
        return services;
    }
}