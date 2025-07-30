using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Permissions;

public record UpdatePermissionCommand(Guid PermissionId, UpdatePermissionDto UpdatePermissionDto) : IRequest<Result<PermissionDto>>;

public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, Result<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IActionRepository _actionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePermissionCommandHandler(
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

    public async Task<Result<PermissionDto>> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId);
        
        if (permission == null)
        {
            return Result<PermissionDto>.Failure("Permission not found");
        }

        var dto = request.UpdatePermissionDto;

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            permission.SetName(dto.Name);
        }

        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            permission.UpdateDescription(dto.Description);
        }

        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value)
            {
                permission.Activate();
            }
            else
            {
                permission.Deactivate();
            }
        }

        await _permissionRepository.UpdateAsync(permission);
        await _unitOfWork.SaveChangesAsync();

        // Get related entities for response
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