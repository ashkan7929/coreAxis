using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.EventBus;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record AdminCreateUserCommand(
    string NationalCode,
    string MobileNumber,
    string BirthDate // yyyymmdd format (e.g., 13791120)
) : IRequest<Result<UserDto>>;

public class AdminCreateUserCommandHandler : IRequestHandler<AdminCreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IShahkarService _shahkarService;
    private readonly ICivilRegistryService _civilRegistryService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AdminCreateUserCommandHandler> _logger;

    public AdminCreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IShahkarService shahkarService,
        ICivilRegistryService civilRegistryService,
        IEventBus eventBus,
        ILogger<AdminCreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _shahkarService = shahkarService;
        _civilRegistryService = civilRegistryService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate duplicates and birth date format
            var validationResult = await ValidateInputAsync(request, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Result<UserDto>.Failure(string.Join(", ", validationResult.Errors));
            }

            // Verify national code and mobile number with Shahkar
            _logger.LogInformation("Admin create-user: Verifying national code {NationalCode} with mobile {Mobile} via Shahkar",
                request.NationalCode, request.MobileNumber);

            var shahkarResult = await _shahkarService.VerifyNationalCodeAndMobileAsync(
                request.NationalCode, request.MobileNumber, cancellationToken);

            if (!shahkarResult.IsSuccess)
            {
                _logger.LogWarning("Shahkar verification failed for {NationalCode}: {Errors}",
                    request.NationalCode, string.Join(", ", shahkarResult.Errors));
                return Result<UserDto>.Failure($"شماره موبایل با کد ملی تطابق ندارد: {string.Join(", ", shahkarResult.Errors)}");
            }

            if (!shahkarResult.Value)
            {
                _logger.LogWarning("Shahkar verification returned false for national code {NationalCode}", request.NationalCode);
                return Result<UserDto>.Failure("شماره موبایل با کد ملی در سامانه شاهکار تطابق ندارد");
            }

            // Get personal information from civil registry
            _logger.LogInformation("Admin create-user: Fetching personal info for national code {NationalCode}", request.NationalCode);
            var civilRegistryResult = await _civilRegistryService.GetPersonalInfoAsync(
                request.NationalCode, request.BirthDate, cancellationToken);

            if (!civilRegistryResult.IsSuccess)
            {
                _logger.LogWarning("Civil registry lookup failed for national code {NationalCode}: {Errors}",
                    request.NationalCode, string.Join(", ", civilRegistryResult.Errors));
                return Result<UserDto>.Failure($"خطا در دریافت اطلاعات از ثبت احوال: {string.Join(", ", civilRegistryResult.Errors)}");
            }

            var personalInfo = civilRegistryResult.Value!;

            // Create username and email based on national code
            var username = request.NationalCode;
            var email = $"{request.NationalCode}@system.local";

            // No password by default for admin-created users
            string? passwordHash = null;

            // Create user entity
            var user = new User(
                username,
                email,
                request.NationalCode,
                request.MobileNumber,
                passwordHash,
                referralCode: null);

            // Update personal info from civil registry
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

            // Mark verifications
            user.VerifyNationalCode();
            user.VerifyMobile();

            // Persist
            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Admin created user {Username} with ID {UserId}", user.Username, user.Id);

            // Map to UserDto
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
                Roles = new()
            };

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during admin create-user for {NationalCode}", request.NationalCode);
            return Result<UserDto>.Failure("خطای غیرمنتظره در ایجاد کاربر توسط ادمین");
        }
    }

    private async Task<Result<bool>> ValidateInputAsync(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if national code already exists
        var existingNationalCode = await _userRepository.GetByNationalCodeAsync(request.NationalCode, cancellationToken);
        if (existingNationalCode != null)
        {
            return Result<bool>.Failure("کد ملی قبلاً ثبت شده است");
        }

        // Check if phone number already exists
        var existingPhone = await _userRepository.GetByPhoneNumberAsync(request.MobileNumber, cancellationToken);
        if (existingPhone != null)
        {
            return Result<bool>.Failure("شماره موبایل قبلاً ثبت شده است");
        }

        // Validate birth date format (yyyymmdd)
        if (request.BirthDate.Length != 8 || !int.TryParse(request.BirthDate, out _))
        {
            return Result<bool>.Failure("فرمت تاریخ تولد نامعتبر است. فرمت صحیح: yyyymmdd (مثل 13791120)");
        }

        if (!DateTime.TryParseExact(request.BirthDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
        {
            return Result<bool>.Failure("تاریخ تولد نامعتبر است. لطفاً تاریخ صحیح وارد کنید.");
        }

        return Result<bool>.Success(true);
    }

    // private string GenerateTemporaryPassword()
    // {
    //     // Simple random password generator
    //     return Guid.NewGuid().ToString("N").Substring(0, 8) + "Aa1!";
    // }
}