using CoreAxis.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.SharedKernel.Observability;

/// <summary>
/// Middleware to map exceptions to RFC7807 Problem+JSON responses.
/// Handles CoreAxisException and generic exceptions uniformly.
/// </summary>
public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var correlationId = context.Items.ContainsKey("CorrelationId") ? context.Items["CorrelationId"]?.ToString() : null;

        var (status, code, title) = MapException(exception);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = "https://tools.ietf.org/html/rfc7807",
            Detail = exception.Message,
            Extensions =
            {
                ["code"] = code,
                ["traceId"] = traceId,
                ["correlationId"] = correlationId
            }
        };

        if (exception is ValidationException ve)
        {
            problem.Extensions["errors"] = ve.Errors;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status ?? StatusCodes.Status500InternalServerError;
        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }

    private static (int? status, string code, string title) MapException(Exception ex)
    {
        return ex switch
        {
            EntityNotFoundException enf => (StatusCodes.Status404NotFound, enf.Code, "Entity Not Found"),
            BusinessRuleViolationException br => (StatusCodes.Status409Conflict, br.Code, "Business Rule Violation"),
            Exceptions.UnauthorizedAccessException ua => (StatusCodes.Status403Forbidden, ua.Code, "Unauthorized"),
            ValidationException ve => (StatusCodes.Status400BadRequest, ve.Code, "Validation Error"),
            CoreAxisException cae => (StatusCodes.Status400BadRequest, cae.Code, "CoreAxis Error"),
            _ => (StatusCodes.Status500InternalServerError, "UNEXPECTED_ERROR", "Unexpected Error")
        };
    }
}