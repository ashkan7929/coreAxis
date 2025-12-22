using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.ApiManager.Application.Security;
using CoreAxis.Modules.ApiManager.Application.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;

namespace CoreAxis.Modules.ApiManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApiManagerApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // HttpContext accessor for correlation/tenant and future auth flows
        services.AddHttpContextAccessor();

        // Memory cache for response and token caching
        services.AddMemoryCache();
        // Optional distributed cache for tokens if app configured it elsewhere
        services.AddDistributedMemoryCache();

        // Add HTTP client with Polly policies
        services.AddHttpClient<IApiProxy, ApiProxy>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5); // Global timeout
        });

        // Register services
        services.AddScoped<IApiProxy, ApiProxy>();

        // Register pluggable authentication handlers and resolver
        services.AddSingleton<IHmacCanonicalSigner, HmacCanonicalSigner>();
        services.AddSingleton<ITimestampProvider, SystemTimestampProvider>();
        
        // Handlers must be Scoped because they consume Scoped services (e.g., ISecretResolver)
        services.AddScoped<IAuthSchemeHandler, ApiKeyAuthHandler>();
        services.AddScoped<IAuthSchemeHandler, OAuth2AuthHandler>();
        services.AddScoped<IAuthSchemeHandler, HmacAuthHandler>();
        services.AddScoped<IAuthSchemeHandlerResolver, AuthSchemeHandlerResolver>();

        // Register masking service for request/response logging
        services.AddSingleton<ILoggingMaskingService, LoggingMaskingService>();

        // Add health checks
        services.AddApiManagerHealthChecks();

        // Rate limiting policy for runtime façade (moved to Web host to avoid assembly issues)
        // To enable, add this in ASP.NET Core host:
        // services.AddRateLimiter(options =>
        // {
        //     options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        //     options.AddPolicy("apim-call", context =>
        //     {
        //         var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "global";
        //         return RateLimitPartition.GetFixedWindowLimiter<string>(
        //             tenantId,
        //             _ => new FixedWindowRateLimiterOptions
        //             {
        //                 PermitLimit = 60,
        //                 Window = TimeSpan.FromMinutes(1),
        //                 QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        //                 QueueLimit = 0,
        //                 AutoReplenishment = true
        //             });
        //     });
        // });

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("Retry {RetryCount} for {OperationKey} in {Delay}ms",
                        retryCount, context.OperationKey, timespan.TotalMilliseconds);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    // Log circuit breaker opened
                },
                onReset: () =>
                {
                    // Log circuit breaker closed
                });
    }
}

public static class PollyContextExtensions
{
    private const string LoggerKey = "ILogger";

    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}