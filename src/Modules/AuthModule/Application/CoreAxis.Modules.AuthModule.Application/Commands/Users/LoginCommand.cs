using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record LoginCommand(
    string Username,
    string Password,
    Guid TenantId,
    string IpAddress
) : IRequest<Result<LoginResultDto>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessLogRepository _accessLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IAccessLogRepository accessLogRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _accessLogRepository = accessLogRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user by username
        var user = await _userRepository.GetByUsernameAsync(request.Username, request.TenantId, cancellationToken);
        if (user == null)
        {
            await LogFailedAttempt(request.Username, request.IpAddress, request.TenantId, "User not found", cancellationToken);
            return Result<LoginResultDto>.Failure("Invalid username or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            await LogFailedAttempt(request.Username, request.IpAddress, request.TenantId, "User inactive", cancellationToken);
            return Result<LoginResultDto>.Failure("Account is inactive");
        }

        // Check if user is locked
        if (user.IsLocked)
        {
            await LogFailedAttempt(request.Username, request.IpAddress, request.TenantId, "User locked", cancellationToken);
            return Result<LoginResultDto>.Failure("Account is locked");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            _userRepository.Update(user);
            await LogFailedAttempt(request.Username, request.IpAddress, request.TenantId, "Invalid password", cancellationToken);
            await _unitOfWork.SaveChangesAsync();
            return Result<LoginResultDto>.Failure("Invalid username or password");
        }

        // Successful login
        user.RecordSuccessfulLogin(request.IpAddress);
        _userRepository.Update(user);

        // Log successful login
        var loginLog = AccessLog.CreateLoginAttempt(user.Username, request.IpAddress, request.TenantId, true, userId: user.Id);
        await _accessLogRepository.AddAsync(loginLog);
        await _unitOfWork.SaveChangesAsync();

        // Generate JWT token
        var token = await _jwtTokenService.GenerateTokenAsync(user, cancellationToken);

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

        var result = new LoginResultDto
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = userDto
        };

        return Result<LoginResultDto>.Success(result);
    }

    private async Task LogFailedAttempt(string username, string ipAddress, Guid tenantId, string reason, CancellationToken cancellationToken)
    {
        var failedLog = AccessLog.CreateLoginAttempt(username, ipAddress, tenantId, false, errorMessage: reason);
        await _accessLogRepository.AddAsync(failedLog);
        await _unitOfWork.SaveChangesAsync();
    }
}