using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest<Result<bool>>;

public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveRoleFromUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        // Validate role exists
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            return Result<bool>.Failure("Role not found");
        }

        // Check if user has this role
        var userRoles = await _userRepository.GetUserRolesAsync(request.UserId);
        if (!userRoles.Any(r => r.Id == request.RoleId))
        {
            return Result<bool>.Failure("User does not have this role");
        }

        // Remove role from user
        await _userRepository.RemoveRoleFromUserAsync(request.UserId, request.RoleId);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}