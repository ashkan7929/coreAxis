using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CoreAxis.SharedKernel.Authorization;

/// <summary>
/// Authorization handler for permission-based authorization.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<RequirePermissionAttribute>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequirePermissionAttribute requirement)
    {
        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }
        
        // Get permissions from claims
        var permissionClaims = context.User.FindAll("permission");
        var requiredPermission = requirement.GetPermissionCode();
        
        // Check if user has the required permission
        foreach (var permissionClaim in permissionClaims)
        {
            if (permissionClaim.Value == requiredPermission)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        
        // Permission not found, authorization fails
        context.Fail();
        return Task.CompletedTask;
    }
}