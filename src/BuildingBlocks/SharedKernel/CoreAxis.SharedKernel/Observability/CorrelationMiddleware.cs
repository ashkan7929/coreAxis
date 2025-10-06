using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Security.Claims;

namespace CoreAxis.SharedKernel.Observability;

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string TenantIdHeader = "X-Tenant-Id";

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var tenantId = GetTenantId(context);
        var userId = GetUserId(context);
        var email = GetUserEmail(context);

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Enrich logs with correlation context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Email", email))
        {
            // Store in HttpContext for easy access
            context.Items["CorrelationId"] = correlationId;
            context.Items["TenantId"] = tenantId;
            context.Items["UserId"] = userId;
            context.Items["Email"] = email;

            _logger.LogDebug("Processing request {Method} {Path} with CorrelationId: {CorrelationId}", 
                context.Request.Method, context.Request.Path, correlationId);

            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) && 
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString();
    }

    private string GetTenantId(HttpContext context)
    {
        // Try to get from header first
        if (context.Request.Headers.TryGetValue(TenantIdHeader, out var tenantId) && 
            !string.IsNullOrEmpty(tenantId))
        {
            return tenantId.ToString();
        }

        // Try to get from JWT claims
        var tenantClaim = context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantClaim))
        {
            return tenantClaim;
        }

        return "default";
    }

    private string? GetUserId(HttpContext context)
    {
        // Try to get from JWT claims
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            return userIdClaim;
        }

        var subClaim = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
        {
            return subClaim;
        }

        return null;
    }

    // Note: Email is personally identifiable information (PII). Avoid logging raw email where not necessary.
    // If logging is required, consider masking or hashing to reduce exposure.
    private string? GetUserEmail(HttpContext context)
    {
        var emailClaim = context.User?.FindFirst(ClaimTypes.Email)?.Value
                         ?? context.User?.FindFirst("email")?.Value;
        return string.IsNullOrEmpty(emailClaim) ? null : emailClaim;
    }
}

public static class CorrelationExtensions
{
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items["CorrelationId"]?.ToString();
    }

    public static string? GetTenantId(this HttpContext context)
    {
        return context.Items["TenantId"]?.ToString();
    }

    public static string? GetUserId(this HttpContext context)
    {
        return context.Items["UserId"]?.ToString();
    }

    public static string? GetUserEmail(this HttpContext context)
    {
        return context.Items["Email"]?.ToString();
    }

    public static Guid GetCorrelationIdAsGuid(this HttpContext context)
    {
        var correlationId = context.GetCorrelationId();
        return Guid.TryParse(correlationId, out var guid) ? guid : Guid.NewGuid();
    }

    public static Guid? GetUserIdAsGuid(this HttpContext context)
    {
        var userId = context.GetUserId();
        return Guid.TryParse(userId, out var guid) ? guid : null;
    }
}