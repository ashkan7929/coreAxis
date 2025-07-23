using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Roles;

public record CreateRoleCommand(
    string Name,
    string Description,
    Guid TenantId,
    List<Guid> PermissionIds
) : IRequest<Result<RoleDto>>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role name already exists
        var existingRole = await _roleRepository.GetByNameAsync(request.Name, request.TenantId, cancellationToken);
        if (existingRole != null)
        {
            return Result<RoleDto>.Failure("Role name already exists");
        }

        // Create role
        var role = new Role(request.Name, request.TenantId, request.Description);
        await _roleRepository.AddAsync(role);

        // Add permissions if provided
        if (request.PermissionIds.Any())
        {
            foreach (var permissionId in request.PermissionIds)
            {
                var permission = await _permissionRepository.GetByIdAsync(permissionId);
                if (permission != null && permission.IsActive)
                {
                    var rolePermission = new RolePermission(role.Id, permissionId, role.Id); // Using role.Id as assignedBy for system assignment
                    await _roleRepository.AddRolePermissionAsync(rolePermission);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Get role with permissions for response
        var roleWithPermissions = await _roleRepository.GetWithPermissionsAsync(role.Id, cancellationToken);
        
        var roleDto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedOn,
            TenantId = role.TenantId ?? Guid.Empty,
            Permissions = roleWithPermissions?.RolePermissions
                .Where(rp => rp.IsActive)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    IsActive = rp.Permission.IsActive,
                    CreatedAt = rp.Permission.CreatedOn
                }).ToList() ?? new List<PermissionDto>()
        };

        return Result<RoleDto>.Success(roleDto);
    }
}