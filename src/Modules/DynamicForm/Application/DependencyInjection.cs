using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Application.Services.Handlers;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreAxis.Modules.DynamicForm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDynamicFormApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Add Application Services
        services.AddScoped<IFormSchemaValidator, FormSchemaValidator>();
        services.AddScoped<IExpressionEngine, ExpressionEngine>();
        services.AddScoped<IDependencyGraph, DependencyGraph>();
        services.AddScoped<IIncrementalRecalculationEngine, IncrementalRecalculationEngine>();
        services.AddScoped<IValidationEngine, ValidationEngine>();
        
        // Add Form Event Services
        services.AddSingleton<IFormEventManager, FormEventManager>();
        services.AddScoped<IFormEventHandler, DefaultFormEventHandler>();

        return services;
    }
}