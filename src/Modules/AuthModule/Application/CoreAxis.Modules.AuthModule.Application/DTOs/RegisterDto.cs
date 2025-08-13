using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;

namespace CoreAxis.Modules.AuthModule.Application.DTOs;

/// <summary>
/// DTO for user registration with national verification
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// National code (کد ملی)
    /// </summary>
    public string NationalCode { get; set; } = string.Empty;

    /// <summary>
    /// Mobile number
    /// </summary>
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>
    /// Birth date in yyyymmdd format (e.g., 13791129)
    /// </summary>
    public string BirthDate { get; set; } = string.Empty;

    /// <summary>
    /// Referral code (optional)
    /// </summary>
    public string? ReferralCode { get; set; }
}

/// <summary>
/// DTO for OTP verification
/// </summary>
public class VerifyOtpDto
{
    /// <summary>
    /// Mobile number
    /// </summary>
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>
    /// OTP code
    /// </summary>
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>
    /// OTP purpose (registration, login, etc.)
    /// </summary>
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Registration;
}

/// <summary>
/// DTO for sending OTP
/// </summary>
public class SendOtpDto
{
    /// <summary>
    /// Mobile number
    /// </summary>
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>
    /// OTP purpose (registration, login, etc.)
    /// </summary>
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
}

/// <summary>
/// DTO for OTP verification result
/// </summary>
public class OtpVerificationResultDto
{
    /// <summary>
    /// Whether OTP verification was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// User information (if login)
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// JWT token (if login)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Token expiration (if login)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for simple OTP verification result (only success status)
/// </summary>
public class SimpleOtpVerificationResultDto
{
    /// <summary>
    /// Whether OTP verification was successful
    /// </summary>
    public bool IsSuccess { get; set; }
}