using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

/// <summary>
/// Command to send OTP to mobile number
/// </summary>
public record SendOtpCommand(
    string MobileNumber,
    OtpPurpose Purpose = OtpPurpose.Login
) : IRequest<Result<string>>;

/// <summary>
/// Handler for SendOtpCommand
/// </summary>
public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Result<string>>
{
    private readonly IOtpService _otpService;
    private readonly IMegfaSmsService _smsService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendOtpCommandHandler> _logger;

    public SendOtpCommandHandler(
        IOtpService otpService,
        IMegfaSmsService smsService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendOtpCommandHandler> logger)
    {
        _otpService = otpService;
        _smsService = smsService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending OTP to mobile number: {MobileNumber} for purpose: {Purpose}", 
                request.MobileNumber, request.Purpose);

            // For login purpose, check if user exists
            if (request.Purpose == OtpPurpose.Login)
            {
                var existingUser = await _userRepository.GetByPhoneNumberAsync(request.MobileNumber, cancellationToken);
                if (existingUser == null)
                {
                    _logger.LogWarning("Login attempt for non-existent mobile number: {MobileNumber}", request.MobileNumber);
                    return Result<string>.Failure("کاربری با این شماره موبایل یافت نشد");
                }

                if (!existingUser.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive user: {MobileNumber}", request.MobileNumber);
                    return Result<string>.Failure("حساب کاربری غیرفعال است");
                }
            }

            // Generate OTP
            var otpResult = await _otpService.GenerateOtpAsync(request.MobileNumber, request.Purpose, cancellationToken);
            if (!otpResult.IsSuccess)
            {
                _logger.LogError("Failed to generate OTP for mobile: {MobileNumber}. Errors: {Errors}", 
                    request.MobileNumber, string.Join(", ", otpResult.Errors));
                return Result<string>.Failure(otpResult.Errors);
            }

            // Save OTP to database
            await _unitOfWork.SaveChangesAsync();

            // Send SMS
            var smsResult = await _smsService.SendOtpAsync(request.MobileNumber, otpResult.Value, cancellationToken);
            if (!smsResult.IsSuccess)
            {
                _logger.LogError("Failed to send OTP SMS to mobile: {MobileNumber}. Errors: {Errors}", 
                    request.MobileNumber, string.Join(", ", smsResult.Errors));
                return Result<string>.Failure($"خطا در ارسال پیامک: {string.Join(", ", smsResult.Errors)}");
            }

            _logger.LogInformation("OTP sent successfully to mobile: {MobileNumber}", request.MobileNumber);
            return Result<string>.Success("کد تأیید با موفقیت ارسال شد");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending OTP to mobile: {MobileNumber}", request.MobileNumber);
            return Result<string>.Failure("خطای غیرمنتظره در ارسال کد تأیید");
        }
    }
}