using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record UpdateUserCommand(Guid UserId, UpdateUserDto UpdateUserDto) : IRequest<Result<UserDto>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        var dto = request.UpdateUserDto;

        // Check email uniqueness if email is being changed
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null && existingUser.Id != request.UserId)
            {
                return Result<UserDto>.Failure("Email already exists");
            }
        }

        // Update profile if email or phone number is provided
        if (!string.IsNullOrWhiteSpace(dto.Email) || dto.PhoneNumber != null)
        {
            var newEmail = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email : user.Email;
            var newPhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.UpdateProfile(user.Username, newEmail, newPhoneNumber);
        }

        // Update active status
        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }
        }

        // Note: EmailConfirmed and PhoneNumberConfirmed properties don't exist in User entity

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hashedPassword = _passwordHasher.HashPassword(dto.Password);
            user.UpdatePassword(hashedPassword);
        }

        // Update roles if provided
        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            // Validate all roles exist
            foreach (var roleId in dto.RoleIds)
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                if (role == null)
                {
                    return Result<UserDto>.Failure($"Role with ID {roleId} not found");
                }
            }

            // Update user roles
            await _userRepository.UpdateUserRolesAsync(request.UserId, dto.RoleIds);
        }

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Get updated user with roles for response
        var userRoles = await _userRepository.GetUserRolesAsync(request.UserId);
        var roleDtos = userRoles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedOn,
            Permissions = new List<PermissionDto>() // Not loading permissions here for performance
        }).ToList();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            IsLocked = user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow,
            CreatedAt = user.CreatedOn,
            LastLoginAt = user.LastLoginAt,
            FailedLoginAttempts = user.FailedLoginAttempts,
            Roles = roleDtos
        };

        return Result<UserDto>.Success(userDto);
    }
}