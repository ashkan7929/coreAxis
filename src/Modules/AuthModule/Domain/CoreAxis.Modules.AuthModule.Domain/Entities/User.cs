using CoreAxis.SharedKernel;
using CoreAxis.Modules.AuthModule.Domain.Events;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class User : EntityBase
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public int FailedLoginAttempts { get; private set; } = 0;
    public DateTime? LockedUntil { get; private set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public virtual ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();
    public virtual ICollection<AccessLog> AccessLogs { get; private set; } = new List<AccessLog>();

    private User() { } // For EF Core

    public User(string username, string email, string passwordHash, string? phoneNumber = null)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        
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
}