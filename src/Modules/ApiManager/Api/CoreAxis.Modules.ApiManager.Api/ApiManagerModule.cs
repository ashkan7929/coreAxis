using CoreAxis.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace CoreAxis.Modules.ApiManager.API;

public class ApiManagerModule : IModule
{
    public string Name => "ApiManager";
    public string Version => "1.0.0";

    /// <summary>
    /// Registers the module's services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    public void RegisterServices(IServiceCollection services)
    {
        // Get configuration from the service provider
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        // Register all ApiManager services
        // services.AddApiManagerModule(configuration); // Commented out to avoid duplicate registration
        
        // Add controllers from this module
        services.AddControllers()
            .AddApplicationPart(typeof(ApiManagerModule).Assembly);

        // Configure rate limiter policy for runtime façade calls
        services.AddRateLimiter(options =>
        {
            // 429 with custom ProblemDetails payload
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata("RetryAfter", out var ra) && ra is TimeSpan ts
                    ? ts.TotalSeconds
                    : 60; // fallback

                context.HttpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter).ToString();
                var problem = new ProblemDetails
                {
                    Type = "https://coreaxis.dev/problems/apim/rate_limited",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please retry later."
                };
                problem.Extensions["code"] = "APIM_RATE_LIMITED";
                problem.Extensions["policy"] = "apim-call";
                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            };

            // Read thresholds from configuration (defaults applied if not present)
            var tenantLimit = int.TryParse(configuration["ApiManager:RateLimiting:TenantLimitPerMinute"], out var t) ? t : 60;
            var windowMinutes = int.TryParse(configuration["ApiManager:RateLimiting:WindowMinutes"], out var w) ? w : 1;

            options.AddPolicy("apim-call", httpContext =>
            {
                var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "global";
                return RateLimitPartition.GetFixedWindowLimiter<string>(
                    tenantId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = tenantLimit,
                        Window = TimeSpan.FromMinutes(windowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        // Configure output caching for GET endpoints (e.g., registry export)
        services.AddOutputCache();
            
        Console.WriteLine($"Module {Name} v{Version} services registered.");
    }

    /// <summary>
    /// Configures the module's middleware and endpoints in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder to configure middleware with.</param>
    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Enable rate limiting middleware for policies configured in services
        app.UseRateLimiter();
        // Enable output caching middleware
        app.UseOutputCache();
        Console.WriteLine($"Module {Name} v{Version} configured.");
    }
}