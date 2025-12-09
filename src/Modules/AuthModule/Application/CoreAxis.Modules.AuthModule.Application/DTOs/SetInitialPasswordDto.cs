namespace CoreAxis.Modules.AuthModule.Application.DTOs;

/// <summary>
/// DTO for setting the initial password using OTP
/// </summary>
public class SetInitialPasswordDto
{
    /// <summary>
    /// Mobile number
    /// </summary>
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>
    /// OTP code received via SMS
    /// </summary>
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
