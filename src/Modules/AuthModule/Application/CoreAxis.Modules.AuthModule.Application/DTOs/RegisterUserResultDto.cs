namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class RegisterUserResultDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string NationalCode { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public bool IsNationalCodeVerified { get; set; }
    public bool IsPersonalInfoVerified { get; set; }
    public bool IsMobileVerified { get; set; }
    public bool RequiresOtpVerification { get; set; }
    public string Message { get; set; } = string.Empty;
}