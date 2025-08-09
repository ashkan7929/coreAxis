using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

/// <summary>
/// Command to verify OTP code
/// </summary>
public record VerifyOtpCommand(
    string MobileNumber,
    string OtpCode,
    OtpPurpose Purpose
) : IRequest<Result<OtpVerificationResultDto>>;

/// <summary>
/// Handler for VerifyOtpCommand
/// </summary>
public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<OtpVerificationResultDto>>
{
    private readonly IOtpService _otpService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;

    public VerifyOtpCommandHandler(
        IOtpService otpService,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<VerifyOtpCommandHandler> logger)
    {
        _otpService = otpService;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<OtpVerificationResultDto>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Verifying OTP for mobile number: {MobileNumber} with purpose: {Purpose}", 
                request.MobileNumber, request.Purpose);

            // Verify OTP
            var verificationResult = await _otpService.VerifyOtpAsync(
                request.MobileNumber, 
                request.OtpCode, 
                request.Purpose, 
                cancellationToken);

            if (!verificationResult.IsSuccess)
            {
                _logger.LogError("OTP verification failed for mobile: {MobileNumber}. Errors: {Errors}", 
                    request.MobileNumber, string.Join(", ", verificationResult.Errors));
                return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                {
                    IsSuccess = false,
                    Message = "کد تأیید نامعتبر است"
                });
            }

            if (!verificationResult.Value)
            {
                _logger.LogWarning("Invalid OTP provided for mobile: {MobileNumber}", request.MobileNumber);
                return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                {
                    IsSuccess = false,
                    Message = "کد تأیید نامعتبر است"
                });
            }

            // Handle different purposes
            if (request.Purpose == OtpPurpose.Login)
            {
                // For login, find user and generate token
                var user = await _userRepository.GetByPhoneNumberAsync(request.MobileNumber, cancellationToken);
                if (user == null)
                {
                    _logger.LogError("User not found for mobile: {MobileNumber} during OTP login", request.MobileNumber);
                    return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "کاربری با این شماره موبایل یافت نشد"
                    });
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Inactive user attempted login: {MobileNumber}", request.MobileNumber);
                    return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "حساب کاربری غیرفعال است"
                    });
                }

                // Get user with roles and permissions
                var userWithPermissions = await _userRepository.GetWithPermissionsAsync(user.Id, cancellationToken);
                if (userWithPermissions == null)
                {
                    _logger.LogError("Failed to load user permissions for user: {UserId}", user.Id);
                    return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "خطا در بارگذاری اطلاعات کاربر"
                    });
                }

                // Generate JWT token
                var tokenResult = await _jwtTokenService.GenerateTokenAsync(user, cancellationToken);

                // Update user login info
                user.RecordSuccessfulLogin("OTP_LOGIN");
                _userRepository.Update(user);

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

                _logger.LogInformation("OTP login successful for mobile: {MobileNumber}", request.MobileNumber);
                return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                {
                    IsSuccess = true,
                    User = userDto,
                    Token = tokenResult.Token,
                    ExpiresAt = tokenResult.ExpiresAt,
                    Message = "ورود با موفقیت انجام شد"
                });
            }
            else if (request.Purpose == OtpPurpose.Registration)
            {
                // For registration, just confirm OTP verification
                _logger.LogInformation("OTP registration verification successful for mobile: {MobileNumber}", request.MobileNumber);
                return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                {
                    IsSuccess = true,
                    Message = "کد تأیید با موفقیت تأیید شد"
                });
            }
            else
            {
                // For other purposes
                _logger.LogInformation("OTP verification successful for mobile: {MobileNumber} with purpose: {Purpose}", 
                    request.MobileNumber, request.Purpose);
                return Result<OtpVerificationResultDto>.Success(new OtpVerificationResultDto
                {
                    IsSuccess = true,
                    Message = "کد تأیید با موفقیت تأیید شد"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while verifying OTP for mobile: {MobileNumber}", request.MobileNumber);
            return Result<OtpVerificationResultDto>.Failure("خطای غیرمنتظره در تأیید کد");
        }
    }
}