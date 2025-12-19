using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;
using CoreAxis.Modules.ProductOrderModule.Application.Services;

namespace CoreAxis.Modules.ProductOrderModule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderModuleApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Add Flow Resolver
        services.AddScoped<IProductFlowResolver, ProductFlowResolver>();

        return services;
    }
}