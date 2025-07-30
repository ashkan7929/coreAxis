using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record AssignRoleCommand(AssignRoleDto AssignRoleDto) : IRequest<Result<bool>>;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var dto = request.AssignRoleDto;

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(dto.UserId);
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        // Validate role exists
        var role = await _roleRepository.GetByIdAsync(dto.RoleId);
        if (role == null)
        {
            return Result<bool>.Failure("Role not found");
        }

        // Check if user already has this role
        var userRoles = await _userRepository.GetUserRolesAsync(dto.UserId);
        var hasRole = userRoles.Any(r => r.Id == dto.RoleId);

        if (dto.IsAssigned && hasRole)
        {
            return Result<bool>.Failure("User already has this role");
        }

        if (!dto.IsAssigned && !hasRole)
        {
            return Result<bool>.Failure("User does not have this role");
        }

        if (dto.IsAssigned)
        {
            // Assign role to user
            await _userRepository.AssignRoleToUserAsync(dto.UserId, dto.RoleId);
        }
        else
        {
            // Remove role from user
            await _userRepository.RemoveRoleFromUserAsync(dto.UserId, dto.RoleId);
        }

        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}