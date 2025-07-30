using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Roles;

public record GetRoleByIdQuery(Guid RoleId) : IRequest<Result<RoleDto>>;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;

    public GetRoleByIdQueryHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
    }

    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        
        if (role == null)
        {
            return Result<RoleDto>.Failure("Role not found");
        }

        // Get role permissions
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