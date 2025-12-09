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
    string? Password
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
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("Username already exists");
        }

        // Check if email already exists
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            return Result<UserDto>.Failure("Email already exists");
        }

        // Hash password if provided
        string? passwordHash = null;
        if (!string.IsNullOrEmpty(request.Password))
        {
            passwordHash = _passwordHasher.HashPassword(request.Password);
        }

        // Create user
        var user = new User(
            request.Username,
            request.Email,
            passwordHash);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain event
        var userRegisteredEvent = new UserRegisteredIntegrationEvent(
            user.Id,
            user.Email,
            user.Username,
            user.Username); // Using username as both FirstName and LastName as placeholders
        
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
            FailedLoginAttempts = user.FailedLoginAttempts
        };

        return Result<UserDto>.Success(userDto);
    }
}