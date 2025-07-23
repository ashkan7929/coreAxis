using CoreAxis.ApiGateway.HealthChecks;
using CoreAxis.ApiGateway.Logging;
using CoreAxis.BuildingBlocks;
using CoreAxis.EventBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;

try
{
    var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CoreAxis API", Version = "v1" });
});

// Register event bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Add health checks
builder.Services.AddCoreAxisHealthChecks();

// Register modules
builder.Services.AddSingleton<IModuleRegistrar, ModuleRegistrar>();
var serviceProvider = builder.Services.BuildServiceProvider();
var moduleRegistrar = serviceProvider.GetRequiredService<IModuleRegistrar>();
var modules = moduleRegistrar.DiscoverAndRegisterModules(builder.Services);

// Register ModuleEnricher for Serilog
builder.Services.AddSingleton<ModuleEnricher>();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("serilog.json")
        .Build())
    .Enrich.FromLogContext()
    .Enrich.With(builder.Services.BuildServiceProvider().GetRequiredService<ModuleEnricher>())
    .CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Configure modules
    var devModules = moduleRegistrar.GetRegisteredModules();
    moduleRegistrar.ConfigureModules(devModules, app);
    
    // Use health checks
    app.UseCoreAxisHealthChecks();
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
