using Microsoft.AspNetCore.Authorization;

namespace CoreAxis.SharedKernel.Authorization;

/// <summary>
/// Authorization attribute that requires specific page and action permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAuthorizationRequirement
{
    /// <summary>
    /// Gets the page code required for authorization.
    /// </summary>
    public string Page { get; }
    
    /// <summary>
    /// Gets the action code required for authorization.
    /// </summary>
    public string Action { get; }
    
    /// <summary>
    /// Initializes a new instance of the RequirePermissionAttribute class.
    /// </summary>
    /// <param name="page">The page code required for authorization.</param>
    /// <param name="action">The action code required for authorization.</param>
    public RequirePermissionAttribute(string page, string action)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
    
    /// <summary>
    /// Gets the permission code in the format "Page:Action".
    /// </summary>
    /// <returns>The permission code.</returns>
    public string GetPermissionCode()
    {
        return $"{Page}:{Action}";
    }
}