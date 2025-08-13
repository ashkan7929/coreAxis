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
        
        // Add JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(configuration["Jwt:SecretKey"] ?? 
                        throw new InvalidOperationException("JWT SecretKey is not configured"))),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        
        // Add Authorization with custom policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        
        // Add Controllers
        services.AddControllers();
        
        return services;
    }
}