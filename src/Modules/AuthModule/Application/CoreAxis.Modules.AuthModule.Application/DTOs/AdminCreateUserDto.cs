namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class AdminCreateUserDto
{
    public string NationalCode { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    // BirthDate should be in yyyymmdd (e.g., 13791120)
    public string BirthDate { get; set; } = string.Empty;
}