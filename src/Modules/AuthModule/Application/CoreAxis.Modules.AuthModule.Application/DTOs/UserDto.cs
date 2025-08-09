namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    // New personal and identification fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? Gender { get; set; }
    public int? CertNumber { get; set; }
    public string? IdentificationSerial { get; set; }
    public string? IdentificationSeri { get; set; }
    public string? OfficeName { get; set; }
    public string? ReferralCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string NationalCode { get; set; } = string.Empty;
    public bool IsMobileVerified { get; set; }
    public bool IsNationalCodeVerified { get; set; }
    public bool IsPersonalInfoVerified { get; set; }
    public string? CivilRegistryTrackId { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailConfirmed { get; set; }
    public bool? PhoneNumberConfirmed { get; set; }
    public string? Password { get; set; }
    public List<Guid>? RoleIds { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}