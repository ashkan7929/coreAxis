using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Roles;

public record AddPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<Result<bool>>;

public class AddPermissionToRoleCommandHandler : IRequestHandler<AddPermissionToRoleCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddPermissionToRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(AddPermissionToRoleCommand request, CancellationToken cancellationToken)
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

        // Check if role already has this permission
        var rolePermissions = await _roleRepository.GetRolePermissionsAsync(request.RoleId);
        if (rolePermissions.Any(rp => rp.PermissionId == request.PermissionId))
        {
            return Result<bool>.Failure("Role already has this permission");
        }

        // Add permission to role
        await _roleRepository.AddPermissionToRoleAsync(request.RoleId, request.PermissionId);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}