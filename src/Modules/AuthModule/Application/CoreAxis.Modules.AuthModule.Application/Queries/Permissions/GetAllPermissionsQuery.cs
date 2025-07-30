using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Permissions;

public record GetAllPermissionsQuery() : IRequest<Result<IEnumerable<PermissionDto>>>;

public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, Result<IEnumerable<PermissionDto>>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;

    public GetAllPermissionsQueryHandler(
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository)
    {
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
    }

    public async Task<Result<IEnumerable<PermissionDto>>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAll().ToListAsync(cancellationToken);
        var pages = await _pageRepository.GetAllActiveAsync(cancellationToken);
        var actions = await _actionRepository.GetAllActiveAsync(cancellationToken);
        
        var permissionDtos = permissions.Select(permission => 
        {
            var page = pages.FirstOrDefault(p => p.Id == permission.PageId);
            var action = actions.FirstOrDefault(a => a.Id == permission.ActionId);
            
            return new PermissionDto
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
            };
        }).ToList();

        return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
    }
}