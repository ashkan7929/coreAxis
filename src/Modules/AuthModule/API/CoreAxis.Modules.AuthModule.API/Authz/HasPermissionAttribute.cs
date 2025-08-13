using Microsoft.AspNetCore.Authorization;

namespace CoreAxis.Modules.AuthModule.API.Authz;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string page, string action)
    {
        Policy = $"perm:{page}:{action}";
    }
}