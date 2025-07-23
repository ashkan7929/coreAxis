using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreAxis.Modules.AuthModule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModuleApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add AutoMapper if needed
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        return services;
    }
}