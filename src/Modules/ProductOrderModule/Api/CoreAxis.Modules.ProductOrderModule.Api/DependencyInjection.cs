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
        
        // Add JWT Authentication (if not already configured globally)
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