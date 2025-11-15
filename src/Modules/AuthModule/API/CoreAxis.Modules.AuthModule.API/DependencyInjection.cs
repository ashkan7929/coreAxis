using CoreAxis.Modules.AuthModule.Application;
using CoreAxis.Modules.AuthModule.Infrastructure;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CoreAxis.Modules.AuthModule.API;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModuleApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.AddAuthModuleApplication();
        services.AddAuthModuleInfrastructure(configuration, env);
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        services.AddControllers();
        return services;
    }
}