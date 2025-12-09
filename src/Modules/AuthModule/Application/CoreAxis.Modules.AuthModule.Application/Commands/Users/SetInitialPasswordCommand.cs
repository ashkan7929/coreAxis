using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using System.Text.RegularExpressions;

namespace CoreAxis.Modules.AuthModule.Application.Commands.Users;

public record SetInitialPasswordCommand(
    string MobileNumber,
    string OtpCode,
    string NewPassword,
    string ConfirmPassword
) : IRequest<Result<bool>>;

public class SetInitialPasswordCommandHandler : IRequestHandler<SetInitialPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

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

        // 3. Verify OTP
        // We use PasswordReset purpose or maybe Login purpose? 
        // User asked to receive OTP via existing endpoint which usually uses Login or Registration.
        // Let's assume we use PasswordReset purpose for setting initial password as well, or Login if checking L151 logic.
        // The user said "receive OTP via .../send-otp", which takes a purpose.
        // If the user uses "Login" purpose there, we should verify "Login" here.
        // However, "PasswordReset" is semantically more correct for setting password.
        // But the requirement says "receive OTP via .../send-otp for users who don't have password".
        // Let's use OtpPurpose.PasswordReset to be safe and logical, assuming the client sends PasswordReset purpose when calling send-otp.
        // OR we can check Login purpose if that's what's expected.
        // Given the context "users who don't have password", they might use "Login" flow to get OTP.
        // Let's check VerifyOtp logic. It takes a purpose.
        // I'll stick to PasswordReset as it's a password operation.
        
        // Wait, if I use PasswordReset, the user must have requested OTP with PasswordReset purpose.
        // If the frontend calls send-otp with "Login", then I must verify "Login".
        // Since this is "Set Initial Password", it's effectively a "Reset Password" flow for a null password.
        // I will use OtpPurpose.PasswordReset for now. If user clarifies, I can change.
        // Actually, looking at the request, it says "receive OTP via .../send-otp".
        // I'll assume the client will send `purpose: PasswordReset` (value 3) when requesting OTP for this action.
        
        var otpResult = await _otpService.VerifyOtpAsync(request.MobileNumber, request.OtpCode, OtpPurpose.PasswordReset, cancellationToken);
        if (!otpResult.IsSuccess || !otpResult.Value)
        {
            // Try Login purpose as fallback if PasswordReset fails? No, that's insecure.
            // Let's enforce PasswordReset purpose.
            return Result<bool>.Failure("کد تایید نامعتبر است");
        }

        // 4. Get User
        var user = await _userRepository.GetByPhoneNumberAsync(request.MobileNumber, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure("کاربر یافت نشد");
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
        await _otpService.InvalidateOtpAsync(request.MobileNumber, OtpPurpose.PasswordReset, cancellationToken);

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
