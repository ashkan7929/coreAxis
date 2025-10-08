using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreAxis.Modules.ApiManager.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddApiManagerPresentation(this IServiceCollection services)
    {
        // Add controllers from this assembly
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly());

        // Add API documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("apimanager-v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "CoreAxis ApiManager API",
                Version = "v1",
                Description = "API for managing external web services and their invocations"
            });

            // Security scheme is centrally registered in ApiGateway to avoid duplicates
            // Modules should only provide their SwaggerDoc entries

            // Include XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}