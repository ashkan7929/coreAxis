using Microsoft.AspNetCore.Authorization;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using System.Security.Claims;

namespace CoreAxis.Modules.AuthModule.API.Authz;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserRepository _userRepository;

    public PermissionAuthorizationHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Fail();
            return;
        }

        var hasPermission = await _userRepository.UserHasPermissionAsync(
            userId, 
            requirement.Page, 
            requirement.Action);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}