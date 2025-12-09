using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using System.Text.RegularExpressions;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record SetInitialPasswordCommand(
    string OtpCode,
    string NewPassword,
    string ConfirmPassword,
    Guid? UserId = null // Optional: if we want to support finding by ID in future, but here we'll get it from token/context
) : IRequest<Result<bool>>;

public class SetInitialPasswordCommandHandler : IRequestHandler<SetInitialPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService; // To get user from context if needed, or we rely on HttpContext accessor in Controller
    // Actually, command should receive UserId or MobileNumber.
    // Since user is authenticated (token), we can pass UserId in command.
    // BUT wait, "users who don't have password" -> they might be logged in via OTP!
    // If they are logged in via OTP, they have a token.
    // So we can get their ID from token.
    // Then we need to find their mobile number to verify OTP (VerifyOtpAsync takes mobile number).
    
    // Let's inject IHttpContextAccessor or similar? No, better to pass UserId in command.
    
    public SetInitialPasswordCommandHandler(
        IUserRepository userRepository,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(SetInitialPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate password match
        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result<bool>.Failure("رمز عبور و تکرار آن مطابقت ندارند");
        }

        // 2. Validate password strength
        if (!IsPasswordStrong(request.NewPassword))
        {
            return Result<bool>.Failure("رمز عبور باید حداقل ۸ کاراکتر و شامل حروف و اعداد باشد");
        }

        // 4. Get User by ID (from token)
        if (request.UserId == null)
        {
             return Result<bool>.Failure("شناسه کاربر نامعتبر است");
        }
        
        var user = await _userRepository.GetByIdAsync(request.UserId.Value);
        if (user == null)
        {
            return Result<bool>.Failure("کاربر یافت نشد");
        }

        // 3. Verify OTP
        // Now we have user.PhoneNumber
        if (string.IsNullOrEmpty(user.PhoneNumber))
        {
             return Result<bool>.Failure("شماره موبایل کاربر یافت نشد");
        }

        var otpResult = await _otpService.VerifyOtpAsync(user.PhoneNumber, request.OtpCode, OtpPurpose.PasswordReset, cancellationToken);
        if (!otpResult.IsSuccess || !otpResult.Value)
        {
            return Result<bool>.Failure("کد تایید نامعتبر است");
        }

        // 5. Check if user already has a password
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            return Result<bool>.Failure("شما قبلاً رمز عبور تعیین کرده‌اید. لطفاً از گزینه تغییر رمز عبور استفاده کنید");
        }

        // 6. Hash and set password
        var passwordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(passwordHash);

        // 7. Save changes
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // 8. Invalidate OTP
        await _otpService.InvalidateOtpAsync(user.PhoneNumber, OtpPurpose.PasswordReset, cancellationToken);

        return Result<bool>.Success(true);
    }

    private bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (password.Length < 8) return false;
        if (!Regex.IsMatch(password, @"[a-zA-Z]")) return false; // Has letters
        if (!Regex.IsMatch(password, @"[0-9]")) return false; // Has digits
        // Optional: Special chars
        return true;
    }
}
