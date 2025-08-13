using CoreAxis.SharedKernel;
using CoreAxis.Modules.AuthModule.Domain.Enums;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class OtpCode : EntityBase
{
    public string MobileNumber { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public OtpPurpose Purpose { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; } = false;
    public int AttemptCount { get; private set; } = 0;
    public DateTime? UsedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private OtpCode() { } // For EF Core

    public OtpCode(
        string mobileNumber, 
        string code, 
        OtpPurpose purpose, 
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        MobileNumber = mobileNumber;
        Code = code;
        Purpose = purpose;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    public void IncrementAttemptCount()
    {
        AttemptCount++;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsExpired && !IsUsed;
}