using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Roles;

public record UpdateRoleCommand(Guid RoleId, UpdateRoleDto UpdateRoleDto) : IRequest<Result<RoleDto>>;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        
        if (role == null)
        {
            return Result<RoleDto>.Failure("Role not found");
        }

        var dto = request.UpdateRoleDto;

        // Update basic fields if provided
        var nameToUpdate = role.Name;
        var descriptionToUpdate = role.Description;
        bool shouldUpdateDetails = false;

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            // Check if name is already taken by another role
            var existingRole = await _roleRepository.GetByNameAsync(dto.Name);
            if (existingRole != null && existingRole.Id != request.RoleId)
            {
                return Result<RoleDto>.Failure("Role name already exists");
            }
            nameToUpdate = dto.Name;
            shouldUpdateDetails = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            descriptionToUpdate = dto.Description;
            shouldUpdateDetails = true;
        }

        if (shouldUpdateDetails)
        {
            role.UpdateDetails(nameToUpdate, descriptionToUpdate);
        }

        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value)
            {
                role.Activate();
            }
            else
            {
                role.Deactivate();
            }
        }

        // Update permissions if provided
        if (dto.PermissionIds != null && dto.PermissionIds.Any())
        {
            // Validate all permissions exist
            var permissions = new List<Domain.Entities.Permission>();
            foreach (var permissionId in dto.PermissionIds)
            {
                var permission = await _permissionRepository.GetByIdAsync(permissionId);
                if (permission == null)
                {
                    return Result<RoleDto>.Failure($"Permission with ID {permissionId} not found");
                }
                permissions.Add(permission);
            }

            // Update role permissions
            await _roleRepository.UpdateRolePermissionsAsync(request.RoleId, dto.PermissionIds);
        }

        await _roleRepository.UpdateAsync(role);
        await _unitOfWork.SaveChangesAsync();

        // Get updated role with permissions for response
        var rolePermissions = await _roleRepository.GetRolePermissionsAsync(request.RoleId);
        var permissionDtos = new List<PermissionDto>();

        foreach (var rolePermission in rolePermissions)
        {
            var permission = rolePermission.Permission;
            var page = await _pageRepository.GetByIdAsync(permission.PageId);
            var action = await _actionRepository.GetByIdAsync(permission.ActionId);

            permissionDtos.Add(new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedOn,
                Page = page != null ? new PageDto
                {
                    Id = page.Id,
                    Code = page.Code,
                    Name = page.Name,
                    Description = page.Description,
                    Path = page.Path,
                    ModuleName = page.ModuleName,
                    IsActive = page.IsActive,
                    SortOrder = page.SortOrder,
                    CreatedAt = page.CreatedOn
                } : new PageDto(),
                Action = action != null ? new ActionDto
                {
                    Id = action.Id,
                    Code = action.Code,
                    Name = action.Name,
                    Description = action.Description,
                    IsActive = action.IsActive,
                    SortOrder = action.SortOrder,
                    CreatedAt = action.CreatedOn
                } : new ActionDto()
            });
        }

        var roleDto = new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedOn,
            Permissions = permissionDtos
        };

        return Result<RoleDto>.Success(roleDto);
    }
}