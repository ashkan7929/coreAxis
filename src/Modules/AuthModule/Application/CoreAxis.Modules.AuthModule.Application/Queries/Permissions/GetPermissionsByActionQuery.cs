using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Permissions;

public record GetPermissionsByActionQuery(Guid ActionId) : IRequest<Result<IEnumerable<PermissionDto>>>;

public class GetPermissionsByActionQueryHandler : IRequestHandler<GetPermissionsByActionQuery, Result<IEnumerable<PermissionDto>>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;

    public GetPermissionsByActionQueryHandler(
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository)
    {
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
    }

    public async Task<Result<IEnumerable<PermissionDto>>> Handle(GetPermissionsByActionQuery request, CancellationToken cancellationToken)
    {
        // Validate action exists
        var action = await _actionRepository.GetByIdAsync(request.ActionId);
        if (action == null)
        {
            return Result<IEnumerable<PermissionDto>>.Failure("Action not found");
        }

        var permissions = await _permissionRepository.GetByActionAsync(request.ActionId);
        var permissionDtos = new List<PermissionDto>();

        foreach (var permission in permissions)
        {
            var page = await _pageRepository.GetByIdAsync(permission.PageId);

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
                Action = new ActionDto
                {
                    Id = action.Id,
                    Code = action.Code,
                    Name = action.Name,
                    Description = action.Description,
                    IsActive = action.IsActive,
                    SortOrder = action.SortOrder,
                    CreatedAt = action.CreatedOn
                }
            });
        }

        return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
    }
}