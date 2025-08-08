using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Application.Services;

public interface IOtpService
{
    /// <summary>
    /// Generates a new OTP code for the specified mobile number
    /// </summary>
    /// <param name="mobileNumber">Mobile phone number</param>
    /// <param name="purpose">Purpose of the OTP (registration, login, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated OTP code</returns>
    Task<Result<string>> GenerateOtpAsync(string mobileNumber, OtpPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the provided OTP code
    /// </summary>
    /// <param name="mobileNumber">Mobile phone number</param>
    /// <param name="otpCode">OTP code to verify</param>
    /// <param name="purpose">Purpose of the OTP</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if OTP is valid</returns>
    Task<Result<bool>> VerifyOtpAsync(string mobileNumber, string otpCode, OtpPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all OTP codes for the specified mobile number and purpose
    /// </summary>
    /// <param name="mobileNumber">Mobile phone number</param>
    /// <param name="purpose">Purpose of the OTP</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task InvalidateOtpAsync(string mobileNumber, OtpPurpose purpose, CancellationToken cancellationToken = default);
}

public enum OtpPurpose
{
    Registration = 1,
    Login = 2,
    PasswordReset = 3,
    PhoneVerification = 4
}

public class OtpCode
{
    public string MobileNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
}