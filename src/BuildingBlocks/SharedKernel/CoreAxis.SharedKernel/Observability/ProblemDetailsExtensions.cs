using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.SharedKernel.Observability;

/// <summary>
/// Extensions to register and use CoreAxis ProblemDetails mapping.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Registers services required for ProblemDetails mapping.
    /// </summary>
    public static IServiceCollection AddCoreAxisProblemDetails(this IServiceCollection services)
    {
        // Currently middleware-based; no special services required beyond logging.
        // Keeping this for discoverability and future expansion.
        return services;
    }

    /// <summary>
    /// Adds the ProblemDetails middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseCoreAxisProblemDetails(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ProblemDetailsMiddleware>();
    }
}