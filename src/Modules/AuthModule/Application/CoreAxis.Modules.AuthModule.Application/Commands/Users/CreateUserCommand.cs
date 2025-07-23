using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Application.IntegrationEvents;
using CoreAxis.Modules.AuthModule.Domain.Events;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.EventBus;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    Guid TenantId
) : IRequest<Result<UserDto>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEventBus _eventBus;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEventBus eventBus)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _eventBus = eventBus;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, request.TenantId, cancellationToken);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("Username already exists");
        }

        // Check if email already exists
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, request.TenantId, cancellationToken);
        if (existingEmail != null)
        {
            return Result<UserDto>.Failure("Email already exists");
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = new User(
            request.Username,
            request.Email,
            passwordHash,
            request.TenantId);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain event
        var userRegisteredEvent = new UserRegisteredIntegrationEvent(
            user.Id,
            user.Username,
            user.Email,
            user.TenantId ?? Guid.Empty);
        
        await _eventBus.PublishAsync(userRegisteredEvent);

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            CreatedAt = user.CreatedOn,
            LastLoginAt = user.LastLoginAt,
            FailedLoginAttempts = user.FailedLoginAttempts,
            TenantId = user.TenantId ?? Guid.Empty
        };

        return Result<UserDto>.Success(userDto);
    }
}