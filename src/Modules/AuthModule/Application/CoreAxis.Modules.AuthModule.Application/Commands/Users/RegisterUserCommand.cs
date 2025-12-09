using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.Application.IntegrationEvents;
using CoreAxis.Modules.AuthModule.Domain.Events;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.EventBus;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedUserRegistered = CoreAxis.SharedKernel.Contracts.Events.UserRegistered;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record RegisterUserCommand(
    string Username,
    string Email,
    string? Password,
    string NationalCode,
    string PhoneNumber,
    string BirthDate, // yyyymmdd format (e.g., 13791120)
    string? ReferralCode = null
) : IRequest<Result<RegisterUserResultDto>>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IShahkarService _shahkarService;
    private readonly ICivilRegistryService _civilRegistryService;
    private readonly IOtpService _otpService;
    private readonly IMegfaSmsService _megfaSmsService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IShahkarService shahkarService,
        ICivilRegistryService civilRegistryService,
        IOtpService otpService,
        IMegfaSmsService megfaSmsService,
        IEventBus eventBus,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _shahkarService = shahkarService;
        _civilRegistryService = civilRegistryService;
        _otpService = otpService;
        _megfaSmsService = megfaSmsService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResultDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            var validationResult = await ValidateInputAsync(request, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Result<RegisterUserResultDto>.Failure(string.Join(", ", validationResult.Errors));
            }

            // Verify national code and mobile number with Shahkar
            _logger.LogInformation("Verifying national code {NationalCode} with mobile {PhoneNumber} via Shahkar",
                request.NationalCode, request.PhoneNumber);

            var shahkarResult = await _shahkarService.VerifyNationalCodeAndMobileAsync(
                request.NationalCode, request.PhoneNumber, cancellationToken);
            _logger.LogInformation("****************", request);

            if (!shahkarResult.IsSuccess)
            {
                _logger.LogWarning("Shahkar verification failed for national code {NationalCode}: {Errors}",
                    request.NationalCode, string.Join(", ", shahkarResult.Errors));
                return Result<RegisterUserResultDto>.Failure($"شماره موبایل با کد ملی تطابق ندارد: {string.Join(", ", shahkarResult.Errors)}");
            }

            if (!shahkarResult.Value)
            {
                _logger.LogWarning("Shahkar verification returned false for national code {NationalCode}", request.NationalCode);
                return Result<RegisterUserResultDto>.Failure("شماره موبایل با کد ملی در سامانه شاهکار تطابق ندارد");
            }

            // Get personal information from civil registry
            _logger.LogInformation("Fetching personal info for national code {NationalCode} from civil registry", request.NationalCode);

            var civilRegistryResult = await _civilRegistryService.GetPersonalInfoAsync(
                request.NationalCode, request.BirthDate, cancellationToken);

            if (!civilRegistryResult.IsSuccess)
            {
                _logger.LogWarning("Civil registry lookup failed for national code {NationalCode}: {Errors}",
                    request.NationalCode, string.Join(", ", civilRegistryResult.Errors));
                return Result<RegisterUserResultDto>.Failure($"خطا در دریافت اطلاعات از ثبت احوال: {string.Join(", ", civilRegistryResult.Errors)}");
            }

            var personalInfo = civilRegistryResult.Value!;

            // Hash password if provided
            string? passwordHash = null;
            if (!string.IsNullOrEmpty(request.Password))
            {
                passwordHash = _passwordHasher.HashPassword(request.Password);
            }

            // Create user with all information
            var user = new User(
                request.Username,
                request.Email,
                request.NationalCode,
                request.PhoneNumber,
                passwordHash,
                request.ReferralCode);

            // Update personal information from civil registry
            // Convert birth date from yyyymmdd format to DateOnly
            if (DateTime.TryParseExact(request.BirthDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                var birthDate = DateOnly.FromDateTime(parsedDate);
                user.UpdatePersonalInfo(
                    personalInfo.FirstName,
                    personalInfo.LastName,
                    personalInfo.FatherName,
                    birthDate,
                    personalInfo.Gender,
                    personalInfo.CertNumber,
                    personalInfo.IdentificationSerial,
                    personalInfo.IdentificationSeri,
                    personalInfo.OfficeName,
                    personalInfo.TrackId);
            }

            // Mark national code as verified (since Shahkar verification passed)
            user.VerifyNationalCode();

            // Generate and send OTP for mobile verification
            var otpResult = await _otpService.GenerateOtpAsync(
                request.PhoneNumber, OtpPurpose.Registration, cancellationToken);

            if (!otpResult.IsSuccess)
            {
                _logger.LogError("Failed to generate OTP for {PhoneNumber}: {Errors}", request.PhoneNumber, string.Join(", ", otpResult.Errors));
                return Result<RegisterUserResultDto>.Failure($"خطا در تولید کد تایید: {string.Join(", ", otpResult.Errors)}");
            }

            // Send OTP via SMS
            var smsResult = await _megfaSmsService.SendOtpAsync(
                request.PhoneNumber, otpResult.Value, cancellationToken);

            if (!smsResult.IsSuccess)
            {
                _logger.LogError("Failed to send OTP SMS to {PhoneNumber}: {Errors}", request.PhoneNumber, string.Join(", ", smsResult.Errors));
                return Result<RegisterUserResultDto>.Failure($"خطا در ارسال پیامک: {string.Join(", ", smsResult.Errors)}");
            }

            // Save user to database
            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {Username} registered successfully with ID {UserId}", request.Username, user.Id);

            // Publish integration event
            var userRegisteredEvent = new UserRegisteredIntegrationEvent(
                user.Id,
                user.Email,
                user.FirstName ?? user.Username,
                user.LastName ?? string.Empty);

            await _eventBus.PublishAsync(userRegisteredEvent);

            // Publish SharedKernel event for other modules (e.g. Wallet)
            var sharedUserRegistered = new SharedUserRegistered(
                user.Id,
                user.Email,
                "Default", // TenantId
                Guid.NewGuid() // CorrelationId
            );
            await _eventBus.PublishAsync(sharedUserRegistered);

            var result = new RegisterUserResultDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                NationalCode = user.NationalCode,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FatherName = user.FatherName,
                IsNationalCodeVerified = user.IsNationalCodeVerified,
                IsPersonalInfoVerified = user.IsPersonalInfoVerified,
                IsMobileVerified = user.IsMobileVerified,
                RequiresOtpVerification = true,
                Message = "ثبت نام با موفقیت انجام شد. کد تایید به شماره موبایل شما ارسال گردید."
            };

            return Result<RegisterUserResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration for {Username}", request.Username);
            return Result<RegisterUserResultDto>.Failure("خطای غیرمنتظره در فرآیند ثبت نام");
        }
    }

    private async Task<Result<bool>> ValidateInputAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUser != null)
        {
            return Result<bool>.Failure("نام کاربری قبلاً استفاده شده است");
        }

        // Check if email already exists
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            return Result<bool>.Failure("ایمیل قبلاً استفاده شده است");
        }

        // Check if national code already exists
        var existingNationalCode = await _userRepository.GetByNationalCodeAsync(request.NationalCode, cancellationToken);
        if (existingNationalCode != null)
        {
            return Result<bool>.Failure("کد ملی قبلاً ثبت شده است");
        }

        // Check if phone number already exists
        var existingPhone = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (existingPhone != null)
        {
            return Result<bool>.Failure("شماره موبایل قبلاً ثبت شده است");
        }

        // Validate birth date format (yyyymmdd)
        if (request.BirthDate.Length != 8 || !int.TryParse(request.BirthDate, out _))
        {
            return Result<bool>.Failure("فرمت تاریخ تولد نامعتبر است. فرمت صحیح: yyyymmdd (مثل 13791120)");
        }
        
        // Try to parse the date in yyyymmdd format
        if (!DateTime.TryParseExact(request.BirthDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
        {
            return Result<bool>.Failure("تاریخ تولد نامعتبر است. لطفاً تاریخ صحیح وارد کنید.");
        }

        return Result<bool>.Success(true);
    }
}