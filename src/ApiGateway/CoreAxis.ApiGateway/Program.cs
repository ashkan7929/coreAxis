using CoreAxis.ApiGateway.HealthChecks;
using CoreAxis.ApiGateway.Logging;
using CoreAxis.BuildingBlocks;
using CoreAxis.EventBus;
using CoreAxis.Modules.AuthModule.API;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.Modules.ProductOrderModule.Api;
using CoreAxis.Modules.ApiManager.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using DotNetEnv;
using AspNetCoreRateLimit;

try
{
    // Load environment variables from .env file if it exists
    var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }

    var builder = WebApplication.CreateBuilder(args);

    // Add environment variables to configuration
    builder.Configuration.AddEnvironmentVariables();

    // Manually expand environment variables in configuration sections
    ExpandEnvironmentVariables(builder.Configuration);

    // Add services to the container.
    builder.Services.AddControllers();
    
    // Add Localization
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizer, Microsoft.Extensions.Localization.StringLocalizer<Program>>();

    // Add Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost8030", policy =>
    {
        policy.WithOrigins("http://localhost:8030")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "CoreAxis API", Version = "v1" });

        // Add JWT Bearer authentication to Swagger
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
    });

    // Register event bus
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

    // Add health checks
    builder.Services.AddCoreAxisHealthChecks();

    // Register modules
    builder.Services.AddSingleton<IModuleRegistrar, ModuleRegistrar>();
    var serviceProvider = builder.Services.BuildServiceProvider();
    var moduleRegistrar = serviceProvider.GetRequiredService<IModuleRegistrar>();

    // Force load AuthModule assembly
    var authModuleAssembly = typeof(CoreAxis.Modules.AuthModule.API.AuthModule).Assembly;
    Console.WriteLine($"AuthModule assembly loaded: {authModuleAssembly.FullName}");

    // Force load WalletModule assembly
    var walletModuleAssembly = typeof(CoreAxis.Modules.WalletModule.Api.WalletModule).Assembly;
    Console.WriteLine($"WalletModule assembly loaded: {walletModuleAssembly.FullName}");

    // Force load ProductOrderModule assembly
    var productOrderModuleAssembly = typeof(CoreAxis.Modules.ProductOrderModule.Api.ProductOrderModule).Assembly;
    Console.WriteLine($"ProductOrderModule assembly loaded: {productOrderModuleAssembly.FullName}");

    // Force load ApiManager assembly
    var apiManagerModuleAssembly = typeof(CoreAxis.Modules.ApiManager.API.ApiManagerModule).Assembly;
    Console.WriteLine($"ApiManager assembly loaded: {apiManagerModuleAssembly.FullName}");

    // Debug: List all loaded assemblies
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
    Console.WriteLine($"Total loaded assemblies: {loadedAssemblies.Length}");
    foreach (var assembly in loadedAssemblies)
    {
        Console.WriteLine($"Assembly: {assembly.GetName().Name}");
    }

    // Register ProductOrderModule
    builder.Services.AddProductOrderModuleApi(builder.Configuration);
    
    var modules = moduleRegistrar.DiscoverAndRegisterModules(builder.Services);
    Console.WriteLine($"Discovered modules: {modules.Count()}");

    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
    builder.Host.UseSerilog();

    var app = builder.Build();
    app.UseCors("AllowLocalhost8030");

    // Configure the HTTP request pipeline.
    // Always enable Swagger for API documentation
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Environment.IsDevelopment())
    {
        // Configure modules
        var devModules = moduleRegistrar.GetRegisteredModules();
        moduleRegistrar.ConfigureModules(devModules, app);

        // Use health checks
        app.UseCoreAxisHealthChecks();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    
    // Use Rate Limiting
    app.UseIpRateLimiting();
    
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Use health checks
    app.UseCoreAxisHealthChecks();

    // Serve static files for health dashboard
    app.UseStaticFiles();

    var summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    app.MapGet("/_diag/env", (IConfiguration cfg) =>
    {
        return Results.Ok(new
        {
            Env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            BaseUrl = cfg["Megfa:BaseUrl"]
        });
    });

    app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

    // Configure modules
    var registeredModules = moduleRegistrar.GetRegisteredModules();
    moduleRegistrar.ConfigureModules(registeredModules, app);

    // Log registered modules
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Registered modules: {ModuleCount}", registeredModules.Count);
    foreach (var module in registeredModules)
    {
        logger.LogInformation("Module: {ModuleName}", module.Name);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void ExpandEnvironmentVariables(IConfiguration configuration)
{
    // Expand environment variables in Magfa section
    var magfaSection = configuration.GetSection("Magfa");
    if (magfaSection.Exists())
    {
        ExpandSection(magfaSection, new[] { "Username", "Password", "Domain", "From", "BaseUrl" });
    }

    // Expand environment variables in Shahkar section
    var shahkarSection = configuration.GetSection("Shahkar");
    if (shahkarSection.Exists())
    {
        ExpandSection(shahkarSection, new[] { "Token" });
    }

    // Expand environment variables in CivilRegistry section
    var civilRegistrySection = configuration.GetSection("CivilRegistry");
    if (civilRegistrySection.Exists())
    {
        ExpandSection(civilRegistrySection, new[] { "Token" });
    }
}

static void ExpandSection(IConfigurationSection section, string[] keys)
{
    foreach (var key in keys)
    {
        var value = section[key];
        if (!string.IsNullOrEmpty(value) && value.StartsWith("${") && value.EndsWith("}"))
        {
            var envVarName = value.Substring(2, value.Length - 3);
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(envValue))
            {
                section[key] = envValue;
            }
        }
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program class accessible for testing
public partial class Program { }
