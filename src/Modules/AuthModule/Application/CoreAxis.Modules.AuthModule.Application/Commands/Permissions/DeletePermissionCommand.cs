using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Permissions;

public record DeletePermissionCommand(Guid PermissionId) : IRequest<Result<bool>>;

public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand, Result<bool>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId);
        
        if (permission == null)
        {
            return Result<bool>.Failure("Permission not found");
        }

        // Check if permission is being used by any roles
        var isInUse = await _permissionRepository.IsPermissionInUseAsync(request.PermissionId);
        if (isInUse)
        {
            return Result<bool>.Failure("Cannot delete permission as it is currently assigned to one or more roles");
        }

        await _permissionRepository.DeleteAsync(permission.Id);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}