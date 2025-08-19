using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.SharedKernel.Authorization;

/// <summary>
/// Extension methods for configuring authorization services.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds permission-based authorization to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy("PermissionPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new RequirePermissionAttribute("Default", "Access"));
            });
        });
        
        return services;
    }
}