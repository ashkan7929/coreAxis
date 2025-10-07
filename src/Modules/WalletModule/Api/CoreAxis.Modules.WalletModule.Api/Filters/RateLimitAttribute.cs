using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace CoreAxis.Modules.WalletModule.Api.Filters;

public class RateLimitAttribute : Attribute, IAsyncActionFilter
{
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();
    private readonly int _count;
    private readonly int _windowSeconds;

    public RateLimitAttribute(int count = 30, int windowSeconds = 60)
    {
        _count = count;
        _windowSeconds = windowSeconds;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.HttpContext.User.FindFirst("sub")?.Value
                    ?? "anonymous";

        var endpoint = context.HttpContext.Request.Path.ToString().ToLowerInvariant();
        var key = $"rate:{userId}:{endpoint}";
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-_windowSeconds);

        var list = _requests.GetOrAdd(key, _ => new List<DateTime>());
        lock (list)
        {
            // Remove old entries
            list.RemoveAll(t => t < windowStart);

            if (list.Count >= _count)
            {
                var problem = new ProblemDetails
                {
                    Title = "Rate limit exceeded",
                    Detail = $"Too many requests. Allowed: {_count} per {_windowSeconds}s",
                    Status = 429,
                    Type = "https://coreaxis.dev/problems/wallet/wlt_rate_limit"
                };

                context.HttpContext.Response.Headers["Retry-After"] = _windowSeconds.ToString();
                problem.Extensions["code"] = "WLT_RATE_LIMIT";
                context.Result = new ObjectResult(problem) { StatusCode = 429 };
                return;
            }

            list.Add(now);
        }

        await next();
    }
}