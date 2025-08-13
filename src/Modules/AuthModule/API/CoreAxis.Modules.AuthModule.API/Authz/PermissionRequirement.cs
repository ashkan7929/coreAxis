using Microsoft.AspNetCore.Authorization;

namespace CoreAxis.Modules.AuthModule.API.Authz;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Page { get; }
    public string Action { get; }

    public PermissionRequirement(string page, string action)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}