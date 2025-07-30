using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Roles;

public record DeleteRoleCommand(Guid RoleId) : IRequest<Result<bool>>;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        
        if (role == null)
        {
            return Result<bool>.Failure("Role not found");
        }

        // Check if role is being used by any users
        var usersWithRole = await _userRepository.GetUsersByRoleIdAsync(request.RoleId);
        if (usersWithRole.Any())
        {
            return Result<bool>.Failure("Cannot delete role as it is currently assigned to one or more users");
        }

        // Remove all role permissions first
        await _roleRepository.RemoveAllRolePermissionsAsync(request.RoleId);
        
        // Delete the role
        await _roleRepository.DeleteAsync(request.RoleId);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}