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

                // Generate JWT token
                var tokenResult = await _jwtTokenService.GenerateTokenAsync(user, cancellationToken);

                // Update user login info
                user.RecordSuccessfulLogin("OTP_LOGIN");
                _userRepository.Update(user);

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