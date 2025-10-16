using CoreAxis.Modules.Workflow.Application.Idempotency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Api.Filters;

public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IIdempotencyService _service;

    public IdempotencyFilter(IIdempotencyService service)
    {
        _service = service;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var key = request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            await next();
            return;
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        var bodyHash = ComputeSha256(body);
        var route = request.Path.ToString();

        var cached = await _service.TryGetAsync(route, key!, bodyHash, context.HttpContext.RequestAborted);
        if (cached.found)
        {
            var result = new ContentResult
            {
                StatusCode = cached.statusCode,
                Content = cached.responseJson,
                ContentType = "application/json"
            };
            context.Result = result;
            return;
        }

        var executed = await next();
        var statusCode = executed.Result switch
        {
            ObjectResult o => o.StatusCode ?? 200,
            ContentResult c => c.StatusCode ?? 200,
            StatusCodeResult s => s.StatusCode,
            _ => context.HttpContext.Response.StatusCode
        };

        string? responseJson = executed.Result switch
        {
            ObjectResult o => JsonSerializer.Serialize(o.Value),
            ContentResult c => c.Content,
            _ => null
        };

        await _service.StoreAsync(route, key!, bodyHash, statusCode, responseJson, context.HttpContext.RequestAborted);
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}