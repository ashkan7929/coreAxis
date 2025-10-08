using CoreAxis.BuildingBlocks;
using CoreAxis.EventBus;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Infrastructure;
using CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreAxis.SharedKernel.Observability;
using System.Threading.RateLimiting;

namespace CoreAxis.Modules.MLMModule.API
{
    /// <summary>
    /// MLMModule implementation that handles multi-level marketing operations including user referrals, commission transactions, and commission rules.
    /// </summary>
    public class MLMModule : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name => "MLMModule";

        /// <summary>
        /// Gets the version of the module.
        /// </summary>
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
            
            // Add controllers from this module
            services.AddControllers()
                .AddApplicationPart(typeof(MLMModule).Assembly);

            // Wire ProblemDetails services for uniform error responses
            CoreAxis.SharedKernel.Observability.ProblemDetailsExtensions.AddCoreAxisProblemDetails(services);

            // Register infrastructure services
            services.AddMLMModuleInfrastructure(configuration);

            // Register application services
            services.AddScoped<IMLMService, MLMService>();
            services.AddScoped<ICommissionCalculationService, CommissionCalculationService>();

            // Configure rate limiter policy for sensitive admin actions (approve/reject/mark-paid)
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var retryAfter = context.Lease.TryGetMetadata("RetryAfter", out var ra) && ra is TimeSpan ts
                        ? ts.TotalSeconds
                        : 60; // fallback

                    context.HttpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter).ToString();
                    var problem = new ProblemDetails
                    {
                        Type = "https://coreaxis.dev/problems/mlm/rate_limited",
                        Title = "Too Many Requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = "Rate limit exceeded for MLM admin action. Please retry later."
                    };
                    problem.Extensions["code"] = "MLM_RATE_LIMITED";
                    problem.Extensions["policy"] = "mlm-actions";
                    await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
                };

                var userLimit = int.TryParse(configuration["MLM:RateLimiting:UserLimitPerMinute"], out var ul) ? ul : 30;
                var windowMinutes = int.TryParse(configuration["MLM:RateLimiting:WindowMinutes"], out var wm) ? wm : 1;

                options.AddPolicy("mlm-actions", httpContext =>
                {
                    var userId = httpContext.User?.FindFirst("sub")?.Value
                                 ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                 ?? "anonymous";

                    return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter<string>(
                        userId,
                        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = userLimit,
                            Window = TimeSpan.FromMinutes(windowMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
            });
        }

        /// <summary>
        /// Configures the module's middleware and endpoints in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder to configure middleware with.</param>
        public void ConfigureApplication(IApplicationBuilder app)
        {
            // Get the event bus from the service provider
            var serviceProvider = app.ApplicationServices;
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();

            // Subscribe to PaymentConfirmed.v1 event
            eventBus.Subscribe<PaymentConfirmed, PaymentConfirmedEventHandler>();

            // Insert correlation and ProblemDetails middlewares for consistent observability
            app.UseCoreAxisCorrelation();
            app.UseCoreAxisProblemDetails();
            // Enable rate limiting middleware so [EnableRateLimiting("mlm-actions")] works
            app.UseRateLimiter();
        }
    }
}