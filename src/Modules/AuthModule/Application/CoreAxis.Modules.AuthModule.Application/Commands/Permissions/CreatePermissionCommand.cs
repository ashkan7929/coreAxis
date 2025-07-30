using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Permissions;

public record CreatePermissionCommand(CreatePermissionDto CreatePermissionDto) : IRequest<Result<PermissionDto>>;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Result<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IPageRepository pageRepository,
        IActionRepository actionRepository,
        IUnitOfWork unitOfWork)
    {
        _permissionRepository = permissionRepository;
        _pageRepository = pageRepository;
        _actionRepository = actionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PermissionDto>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.CreatePermissionDto;

        // Validate page exists
        var page = await _pageRepository.GetByIdAsync(dto.PageId);
        if (page == null)
        {
            return Result<PermissionDto>.Failure("Page not found");
        }

        // Validate action exists
        var action = await _actionRepository.GetByIdAsync(dto.ActionId);
        if (action == null)
        {
            return Result<PermissionDto>.Failure("Action not found");
        }

        // Check if permission already exists for this page-action combination
        var existingPermission = await _permissionRepository.GetByPageAndActionAsync(dto.PageId, dto.ActionId);
        if (existingPermission != null)
        {
            return Result<PermissionDto>.Failure("Permission already exists for this page and action combination");
        }

        var permission = new Permission(
            dto.PageId,
            dto.ActionId,
            dto.Description ?? $"Permission to {action.Name} on {page.Name}"
        );
        
        permission.SetName(dto.Name ?? $"{page.Name}_{action.Name}");
        permission.Activate();

        await _permissionRepository.AddAsync(permission);
        await _unitOfWork.SaveChangesAsync();

        var permissionDto = new PermissionDto
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
        };

        return Result<PermissionDto>.Success(permissionDto);
    }
}