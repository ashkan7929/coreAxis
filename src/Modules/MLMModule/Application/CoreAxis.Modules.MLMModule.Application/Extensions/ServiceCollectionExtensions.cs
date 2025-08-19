using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.SharedKernel;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreAxis.Modules.MLMModule.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMLMModuleApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register SharedKernel services
        services.AddSharedKernel();

        // Register application services
        services.AddScoped<IMLMService, MLMService>();
        services.AddScoped<ICommissionCalculationService, CommissionCalculationService>();
        services.AddScoped<ICommissionRuleSetService, CommissionRuleSetService>();

        return services;
    }
}