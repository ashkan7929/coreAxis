using CoreAxis.SharedKernel;
using CoreAxis.Modules.AuthModule.Domain.Events;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class User : EntityBase
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string NationalCode { get; private set; } = string.Empty;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? FatherName { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? ReferralCode { get; private set; }
    public bool IsMobileVerified { get; private set; } = false;
    public bool IsNationalCodeVerified { get; private set; } = false;
    public bool IsPersonalInfoVerified { get; private set; } = false;
    public string? CivilRegistryTrackId { get; private set; }
    public int? Gender { get; private set; }
    public int? CertNumber { get; private set; }
    public string? IdentificationSerial { get; private set; }
    public string? IdentificationSeri { get; private set; }
    public string? OfficeName { get; private set; }
    public string? PasswordHash { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public int FailedLoginAttempts { get; private set; } = 0;
    public DateTime? LockedUntil { get; private set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public virtual ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();
    public virtual ICollection<AccessLog> AccessLogs { get; private set; } = new List<AccessLog>();

    private User() { } // For EF Core

    public User(string username, string email, string? passwordHash = null, string? phoneNumber = null)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        
        AddDomainEvent(new UserRegisteredEvent(Id, username, email));
    }

    public User(string username, string email, string nationalCode, string phoneNumber, string? passwordHash = null, string? referralCode = null)
    {
        Username = username;
        Email = email;
        NationalCode = nationalCode;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        ReferralCode = referralCode;
        
        AddDomainEvent(new UserRegisteredEvent(Id, username, email));
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void UpdateProfile(string username, string email, string? phoneNumber = null)
    {
        Username = username;
        Email = email;
        PhoneNumber = phoneNumber;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void UpdatePersonalInfo(string firstName, string lastName, string? fatherName, DateOnly? birthDate, int? gender, int? certNumber, string? identificationSerial, string? identificationSeri, string? officeName, string? trackId)
    {
        FirstName = firstName;
        LastName = lastName;
        FatherName = fatherName;
        BirthDate = birthDate;
        Gender = gender;
        CertNumber = certNumber;
        IdentificationSerial = identificationSerial;
        IdentificationSeri = identificationSeri;
        OfficeName = officeName;
        CivilRegistryTrackId = trackId;
        IsPersonalInfoVerified = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void VerifyMobile()
    {
        IsMobileVerified = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void VerifyNationalCode()
    {
        IsNationalCodeVerified = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void RecordSuccessfulLogin(string ipAddress)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
        LastModifiedOn = DateTime.UtcNow;
    }

    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void UnlockAccount()
    {
        LockedUntil = null;
        FailedLoginAttempts = 0;
        LastModifiedOn = DateTime.UtcNow;
    }

    public bool IsFullyVerified => IsMobileVerified && IsNationalCodeVerified && IsPersonalInfoVerified;
}