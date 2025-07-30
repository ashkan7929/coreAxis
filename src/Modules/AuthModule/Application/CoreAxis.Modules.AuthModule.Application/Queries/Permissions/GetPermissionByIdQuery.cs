using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Queries.Permissions;

public record GetPermissionByIdQuery(Guid PermissionId) : IRequest<Result<PermissionDto>>;

public class GetPermissionByIdQueryHandler : IRequestHandler<GetPermissionByIdQuery, Result<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;

    public GetPermissionByIdQueryHandler(
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository)
    {
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
    }

    public async Task<Result<PermissionDto>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId);
        
        if (permission == null)
        {
            return Result<PermissionDto>.Failure("Permission not found");
        }

        var page = await _pageRepository.GetByIdAsync(permission.PageId);
        var action = await _actionRepository.GetByIdAsync(permission.ActionId);

        var permissionDto = new PermissionDto
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

        return Result<PermissionDto>.Success(permissionDto);
    }
}