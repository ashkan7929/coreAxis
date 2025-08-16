using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreAxis.Modules.ProductOrderModule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderModuleApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}