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

            // Add JWT security definitions for Swagger
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

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