using CoreAxis.Modules.AuthModule.Application;
using CoreAxis.Modules.AuthModule.Infrastructure;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CoreAxis.Modules.AuthModule.API;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModuleApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Application layer
        services.AddAuthModuleApplication();
        
        // Add Infrastructure layer
        services.AddAuthModuleInfrastructure(configuration);
        
        // JWT Authentication is configured globally in API Gateway
        // No need to configure it here to avoid "Scheme already exists" error
        
        // Add Authorization with custom policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        
        // Add Controllers
        services.AddControllers();
        
        return services;
    }
}