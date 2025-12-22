using Microsoft.AspNetCore.Http;
using System;

namespace CoreAxis.SharedKernel.Observability;

public class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            return context.GetCorrelationIdAsGuid();
        }
        return Guid.NewGuid();
    }
}
