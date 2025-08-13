using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record LoginCommand(
    string MobileNumber,
    string Password,
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
        // Get user by mobile number
        var user = await _userRepository.GetByPhoneNumberAsync(request.MobileNumber, cancellationToken);
        if (user == null)
        {
            await LogFailedAttempt(request.MobileNumber, request.IpAddress, "User not found", cancellationToken);
            return Result<LoginResultDto>.Failure("Invalid mobile number or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            await LogFailedAttempt(request.MobileNumber, request.IpAddress, "User inactive", cancellationToken);
            return Result<LoginResultDto>.Failure("Account is inactive");
        }

        // Check if user is locked
        if (user.IsLocked)
        {
            await LogFailedAttempt(request.MobileNumber, request.IpAddress, "User locked", cancellationToken);
            return Result<LoginResultDto>.Failure("Account is locked");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            _userRepository.Update(user);
            await LogFailedAttempt(request.MobileNumber, request.IpAddress, "Invalid password", cancellationToken);
            await _unitOfWork.SaveChangesAsync();
            return Result<LoginResultDto>.Failure("Invalid mobile number or password");
        }

        // Successful login
        user.RecordSuccessfulLogin(request.IpAddress);
        _userRepository.Update(user);

        // Log successful login
        var loginLog = AccessLog.CreateLoginAttempt(user.Username, request.IpAddress, true, userId: user.Id);
        await _accessLogRepository.AddAsync(loginLog);
        await _unitOfWork.SaveChangesAsync();

        // Get user with roles and permissions
        var userWithPermissions = await _userRepository.GetWithPermissionsAsync(user.Id, cancellationToken);
        if (userWithPermissions == null)
        {
            return Result<LoginResultDto>.Failure("Failed to load user permissions");
        }

        // Generate JWT token
        var token = await _jwtTokenService.GenerateTokenAsync(user, cancellationToken);

        // Map roles with permissions
        var roleDtos = userWithPermissions.UserRoles.Select(ur => new RoleDto
        {
            Id = ur.Role.Id,
            Name = ur.Role.Name,
            Description = ur.Role.Description,
            IsActive = ur.Role.IsActive,
            IsSystemRole = ur.Role.IsSystemRole,
            CreatedAt = ur.Role.CreatedOn,
            Permissions = ur.Role.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                IsActive = rp.Permission.IsActive,
                CreatedAt = rp.Permission.CreatedOn,
                Page = rp.Permission.Page != null ? new PageDto
                {
                    Id = rp.Permission.Page.Id,
                    Code = rp.Permission.Page.Code,
                    Name = rp.Permission.Page.Name,
                    Description = rp.Permission.Page.Description,
                    Path = rp.Permission.Page.Path,
                    ModuleName = rp.Permission.Page.ModuleName,
                    IsActive = rp.Permission.Page.IsActive,
                    SortOrder = rp.Permission.Page.SortOrder,
                    CreatedAt = rp.Permission.Page.CreatedOn
                } : null,
                Action = rp.Permission.Action != null ? new ActionDto
                {
                    Id = rp.Permission.Action.Id,
                    Code = rp.Permission.Action.Code,
                    Name = rp.Permission.Action.Name,
                    Description = rp.Permission.Action.Description,
                    IsActive = rp.Permission.Action.IsActive,
                    SortOrder = rp.Permission.Action.SortOrder,
                    CreatedAt = rp.Permission.Action.CreatedOn
                } : null
            }).ToList()
        }).ToList();

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
            FirstName = user.FirstName,
            LastName = user.LastName,
            FatherName = user.FatherName,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            CertNumber = user.CertNumber,
            IdentificationSerial = user.IdentificationSerial,
            IdentificationSeri = user.IdentificationSeri,
            OfficeName = user.OfficeName,
            ReferralCode = user.ReferralCode,
            PhoneNumber = user.PhoneNumber,
            NationalCode = user.NationalCode,
            IsMobileVerified = user.IsMobileVerified,
            IsNationalCodeVerified = user.IsNationalCodeVerified,
            IsPersonalInfoVerified = user.IsPersonalInfoVerified,
            CivilRegistryTrackId = user.CivilRegistryTrackId,
            Roles = roleDtos
        };

        var result = new LoginResultDto
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = userDto
        };

        return Result<LoginResultDto>.Success(result);
    }

    private async Task LogFailedAttempt(string username, string ipAddress, string reason, CancellationToken cancellationToken)
    {
        var failedLog = AccessLog.CreateLoginAttempt(username, ipAddress, false, errorMessage: reason);
        await _accessLogRepository.AddAsync(failedLog);
        await _unitOfWork.SaveChangesAsync();
    }
}