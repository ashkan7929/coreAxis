using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Roles;

public record RemovePermissionFromRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<Result<bool>>;

public class RemovePermissionFromRoleCommandHandler : IRequestHandler<RemovePermissionFromRoleCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemovePermissionFromRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        // Validate role exists
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            return Result<bool>.Failure("Role not found");
        }

        // Validate permission exists
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId);
        if (permission == null)
        {
            return Result<bool>.Failure("Permission not found");
        }

        // Check if role has this permission
        var rolePermissions = await _roleRepository.GetRolePermissionsAsync(request.RoleId);
        if (!rolePermissions.Any(rp => rp.PermissionId == request.PermissionId))
        {
            return Result<bool>.Failure("Role does not have this permission");
        }

        // Remove permission from role
        await _roleRepository.RemovePermissionFromRoleAsync(request.RoleId, request.PermissionId);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}