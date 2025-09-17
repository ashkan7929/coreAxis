using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MediatR;
using FluentValidation;
using CoreAxis.Modules.ApiManager.Application.Behaviors;

namespace CoreAxis.Modules.ApiManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApiManagerApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}