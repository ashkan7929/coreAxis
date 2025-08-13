namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class CheckMobileDto
{
    public string MobileNumber { get; set; } = string.Empty;
}

public class CheckMobileResultDto
{
    public bool UserExists { get; set; }
    public bool HasPassword { get; set; }
}