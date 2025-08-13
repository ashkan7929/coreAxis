using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CoreAxis.Modules.AuthModule.API.Authz;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName.Split(':');
            if (parts.Length == 3)
            {
                var page = parts[1];
                var action = parts[2];

                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(page, action))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}