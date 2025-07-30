using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Permissions;

public record GetPermissionsByPageQuery(Guid PageId) : IRequest<Result<IEnumerable<PermissionDto>>>;

public class GetPermissionsByPageQueryHandler : IRequestHandler<GetPermissionsByPageQuery, Result<IEnumerable<PermissionDto>>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;

    public GetPermissionsByPageQueryHandler(
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository)
    {
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
    }

    public async Task<Result<IEnumerable<PermissionDto>>> Handle(GetPermissionsByPageQuery request, CancellationToken cancellationToken)
    {
        // Validate page exists
        var page = await _pageRepository.GetByIdAsync(request.PageId);
        if (page == null)
        {
            return Result<IEnumerable<PermissionDto>>.Failure("Page not found");
        }

        var permissions = await _permissionRepository.GetByPageAsync(request.PageId);
        var permissionDtos = new List<PermissionDto>();

        foreach (var permission in permissions)
        {
            var action = await _actionRepository.GetByIdAsync(permission.ActionId);

            permissionDtos.Add(new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedOn,
                Page = new PageDto
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
                },
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

        return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
    }
}