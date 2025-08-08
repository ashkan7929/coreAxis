using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Application.Services;

public interface IMegfaSmsService
{
    /// <summary>
    /// Sends OTP code to the specified mobile number
    /// </summary>
    /// <param name="mobileNumber">Mobile phone number</param>
    /// <param name="otpCode">OTP code to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if SMS was sent successfully</returns>
    Task<Result<SmsResult>> SendOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a custom SMS message to the specified mobile number
    /// </summary>
    /// <param name="mobileNumber">Mobile phone number</param>
    /// <param name="message">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if SMS was sent successfully</returns>
    Task<Result<SmsResult>> SendSmsAsync(string mobileNumber, string message, CancellationToken cancellationToken = default);
}

public class SmsResult
{
    public bool IsSuccess { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}