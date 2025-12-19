using CoreAxis.Modules.MappingModule.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.MappingModule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMappingModuleApplication(this IServiceCollection services)
    {
        services.AddScoped<ITransformEngine, TransformEngine>();
        services.AddScoped<IMappingExecutionService, MappingExecutionService>();
        return services;
    }
}
