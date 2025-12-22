using Microsoft.AspNetCore.Http;
using System.Linq;

namespace CoreAxis.SharedKernel.Context;

public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return null;
            }

            // Priority 1: Authenticated User Claim
            var claim = context.User?.FindFirst("tenant_id")?.Value 
                     ?? context.User?.FindFirst("TenantId")?.Value;
            
            if (!string.IsNullOrEmpty(claim))
            {
                return claim;
            }

            // Priority 2: Header (useful for service-to-service or debugging)
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue))
            {
                return headerValue.ToString();
            }

            return null;
        }
    }
}
