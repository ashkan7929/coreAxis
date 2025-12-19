using CoreAxis.ApiGateway.HealthChecks;
using CoreAxis.ApiGateway.Logging;
using CoreAxis.BuildingBlocks;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Eventing;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.Modules.AuthModule.API;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.Modules.ProductOrderModule.Api;
using CoreAxis.Modules.ApiManager.API;
using CoreAxis.Modules.MappingModule.Api;
using static CoreAxis.Modules.ApiManager.API.DependencyInjection;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
 
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using DotNetEnv;
using AspNetCoreRateLimit;
using System.Text.Json.Serialization;

try
{
    // Load environment variables from .env file if it exists (search common locations)
    string? loadedEnvPath = null;
    
    // Look for .env in typical locations, including current directory and parents
    var currentDir = Directory.GetCurrentDirectory();
    var candidateEnvPaths = new List<string>
    {
        Path.Combine(currentDir, ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env")
    };

    // Add parent directories up to 5 levels
    var parent = Directory.GetParent(currentDir);
    for (int i = 0; i < 5; i++)
    {
        if (parent == null) break;
        candidateEnvPaths.Add(Path.Combine(parent.FullName, ".env"));
        parent = parent.Parent;
    }

    // Add specific path for local dev if needed (optional)
    // candidateEnvPaths.Add("/Users/ashkan/Desktop/projects/coreAxis/.env");

    foreach (var candidate in candidateEnvPaths.Distinct())
    {
        if (File.Exists(candidate))
        {
            Env.Load(candidate);
            loadedEnvPath = candidate;
            break;
        }
    }
    Console.WriteLine(loadedEnvPath != null
        ? $"[Startup] .env loaded from: {loadedEnvPath}"
        : "[Startup] .env not found in search paths; relying on process environment variables.");

    var builder = WebApplication.CreateBuilder(args);

    // In development, bind to a non-conflicting port (avoid macOS AirPlay/AirTunes on 5000)
    if (builder.Environment.IsDevelopment())
    {
        builder.WebHost.UseUrls("http://localhost:5077");
    }

    // Add environment variables to configuration
    builder.Configuration.AddEnvironmentVariables();
    // Load Serilog configuration file
    builder.Configuration.AddJsonFile("serilog.json", optional: true, reloadOnChange: true);

    // Manually expand environment variables in configuration sections
    ExpandEnvironmentVariables(builder.Configuration);

    // Add services to the container.
    // Make DynamicForm optional at runtime to avoid hard failure if assembly is missing
    var mvcBuilder = builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Accept enum values as strings (e.g., "Active" instead of 0)
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    
    
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
        options.AddPolicy("DefaultCorsPolicy", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:8030",
                    "https://vemclub.com",
                    "https://panel.vemclub.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "CoreAxis API Gateway",
            Version = "v1",
            Description = "API Gateway for CoreAxis modular application"
        });

        // Add JWT authentication to Swagger
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

        // Include all controllers in the main doc
        c.DocInclusionPredicate((docName, apiDesc) => true);

        // Include XML comments from all assemblies copied to output for richer docs
        foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                c.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }
            catch
            {
                // ignore malformed XML docs
            }
        }
    });

    // Register event bus
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
    
    // Register DomainEventDispatcher
    builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

    // Add JWT Authentication (centralized configuration)
    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? 
                    throw new InvalidOperationException("JWT SecretKey is not configured"))),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    
    // Add Authorization
    builder.Services.AddAuthorization();

    builder.Services.AddCoreAxisHealthChecks();
    builder.Services.AddHealthChecks()
        .AddCheck<DbConnectivityHealthCheck>("db-connectivity", tags: new[] { "ready" });

    // Register modules
    builder.Services.AddSingleton<IModuleRegistrar, ModuleRegistrar>();
    var serviceProvider = builder.Services.BuildServiceProvider();
    var moduleRegistrar = serviceProvider.GetRequiredService<IModuleRegistrar>();

    // Force load AuthModule assembly
    var authModuleAssembly = typeof(CoreAxis.Modules.AuthModule.API.AuthModule).Assembly;
    var productBuilderModuleAssembly = typeof(CoreAxis.Modules.ProductBuilderModule.Api.ProductBuilderModule).Assembly;
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

    // Force load Workflow module assembly
    var workflowModuleAssembly = typeof(CoreAxis.Modules.Workflow.Api.WorkflowModule).Assembly;
    Console.WriteLine($"Workflow assembly loaded: {workflowModuleAssembly.FullName}");

    // Force load TaskModule assembly
    var taskModuleAssembly = typeof(CoreAxis.Modules.TaskModule.Api.TaskModule).Assembly;
    Console.WriteLine($"TaskModule assembly loaded: {taskModuleAssembly.FullName}");

    // Force load MappingModule assembly
    var mappingModuleAssembly = typeof(CoreAxis.Modules.MappingModule.Api.MappingModule).Assembly;
    Console.WriteLine($"MappingModule assembly loaded: {mappingModuleAssembly.FullName}");

    // Debug: List all loaded assemblies
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
    Console.WriteLine($"Total loaded assemblies: {loadedAssemblies.Length}");
    foreach (var assembly in loadedAssemblies)
    {
        Console.WriteLine($"Assembly: {assembly.GetName().Name}");
    }

    // Register CoreAxis EventBus and auto-load integration handlers
    builder.Services.AddCoreAxisEventBus();

    // Register AuthModule
    builder.Services.AddAuthModuleApi(builder.Configuration, builder.Environment);
    
    // Register WalletModule
    builder.Services.AddWalletModuleApi(builder.Configuration);
    
    // Register ProductOrderModule
    builder.Services.AddProductOrderModuleApi(builder.Configuration);
    
    // Register ApiManager Module
    builder.Services.AddApiManagerModule(builder.Configuration);
    
    var modules = moduleRegistrar.DiscoverAndRegisterModules(builder.Services);
    Console.WriteLine($"Discovered modules: {modules.Count()}");

    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
    builder.Host.UseSerilog();

    var app = builder.Build();

    // Add Correlation and ProblemDetails middleware early in the pipeline
    app.UseMiddleware<CoreAxis.SharedKernel.Observability.CorrelationMiddleware>();
    app.UseMiddleware<CoreAxis.SharedKernel.Observability.ProblemDetailsMiddleware>();

    app.UseCors("DefaultCorsPolicy");

    // Honor PathBase when hosted behind IIS virtual directories or reverse proxies
    var pathBase = builder.Configuration["PathBase"];
    if (!string.IsNullOrEmpty(pathBase))
    {
        app.UsePathBase(pathBase);
    }

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "CoreAxis API V1");
        c.RoutePrefix = "swagger";
    });

    if (app.Environment.IsDevelopment())
    {
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
    app.MapCoreAxisHealthChecks();

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
            BaseUrl = cfg["Magfa:BaseUrl"],
            Username = cfg["Magfa:Username"],
            Domain = cfg["Magfa:Domain"],
            From = cfg["Magfa:From"]
        });
    });

    app.MapGet("/_diag/db", async (IServiceProvider sp) =>
    {
        using var scope = sp.CreateScope();
        var auth = scope.ServiceProvider.GetService<AuthDbContext>();
        var workflow = scope.ServiceProvider.GetService<WorkflowDbContext>();
        var authOk = auth != null && await auth.Database.CanConnectAsync();
        var workflowOk = workflow != null && await workflow.Database.CanConnectAsync();
        var ok = authOk && workflowOk;
        return ok
            ? Results.Ok(new { auth = authOk, workflow = workflowOk })
            : Results.Problem(title: "db-connectivity", detail: $"auth={authOk}, workflow={workflowOk}", statusCode: 503);
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

    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        var dbLogger = app.Services.GetRequiredService<ILogger<Program>>();
        var authDb = sp.GetService<AuthDbContext>();
        var workflowDb = sp.GetService<WorkflowDbContext>();

        if (authDb != null)
        {
            try
            {
                if (authDb.Database.CanConnect())
                {
                    authDb.Database.Migrate();
                    dbLogger.LogInformation("AuthDb migrated: {CanConnect}", authDb.Database.CanConnect());
                }
                else
                {
                    dbLogger.LogWarning("AuthDb unreachable");
                }
            }
            catch (Exception ex)
            {
                dbLogger.LogError(ex, "AuthDb migration failed");
            }
        }

        if (workflowDb != null)
        {
            try
            {
                if (workflowDb.Database.CanConnect())
                {
                    workflowDb.Database.Migrate();
                    dbLogger.LogInformation("WorkflowDb migrated: {CanConnect}", workflowDb.Database.CanConnect());
                }
                else
                {
                    dbLogger.LogWarning("WorkflowDb unreachable");
                }
            }
            catch (Exception ex)
            {
                dbLogger.LogError(ex, "WorkflowDb migration failed");
            }
        }
    }

    // Log registered modules
    Console.WriteLine("About to get logger service...");
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    Console.WriteLine("Logger service obtained successfully.");
    Console.WriteLine("About to log registered modules...");
    logger.LogInformation("Registered modules: {ModuleCount}", registeredModules.Count);
    Console.WriteLine("Logged module count.");
    foreach (var module in registeredModules)
    {
        logger.LogInformation("Module: {ModuleName}", module.Name);
    }
    Console.WriteLine("Finished logging all modules.");

    Console.WriteLine("About to log starting message...");
    logger.LogInformation("Starting CoreAxis API Gateway on http://localhost:5000");
    Console.WriteLine("Starting message logged.");
    
    try
    {
        Console.WriteLine("About to call app.Run()...");
        app.Run();
        Console.WriteLine("app.Run() completed.");
    }
    catch (Exception runEx)
    {
        Console.WriteLine($"Exception in app.Run(): {runEx.Message}");
        logger.LogError(runEx, "Error occurred while running the application");
        throw;
    }
}
catch (Exception ex)
{
    // Ignore HostAbortedException during design-time tools execution
    if (ex.GetType().Name != "HostAbortedException")
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
    }
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
