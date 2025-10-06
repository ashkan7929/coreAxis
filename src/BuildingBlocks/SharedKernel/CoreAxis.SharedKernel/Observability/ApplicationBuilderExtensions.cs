using Microsoft.AspNetCore.Builder;

namespace CoreAxis.SharedKernel.Observability;

/// <summary>
/// ApplicationBuilder extensions for inserting correlation middleware early in the pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Inserts <see cref="CorrelationMiddleware"/> early in the pipeline to propagate/assign
    /// X-Correlation-Id and X-Tenant-Id headers and enrich logging context with
    /// CorrelationId, TenantId, UserId, and Email.
    /// 
    /// Ordering: place this BEFORE authentication/authorization and other middlewares
    /// so that correlation and tenant context are available to them.
    /// </summary>
    public static IApplicationBuilder UseCoreAxisCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationMiddleware>();
    }
}