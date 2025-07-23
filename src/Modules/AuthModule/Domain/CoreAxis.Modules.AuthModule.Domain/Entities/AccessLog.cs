using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class AccessLog : EntityBase
{
    public Guid? UserId { get; private set; }
    public string? Username { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Resource { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? AdditionalData { get; private set; }

    // Navigation properties
    public virtual User? User { get; private set; }

    private AccessLog() { } // For EF Core

    public AccessLog(
        string action,
        string ipAddress,
        Guid tenantId,
        bool isSuccess,
        Guid? userId = null,
        string? username = null,
        string? resource = null,
        string? userAgent = null,
        string? errorMessage = null,
        string? additionalData = null)
    {
        UserId = userId;
        Username = username;
        Action = action;
        Resource = resource;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        TenantId = tenantId;
        AdditionalData = additionalData;
        Timestamp = DateTime.UtcNow;
    }

    public static AccessLog CreateLoginAttempt(string username, string ipAddress, Guid tenantId, bool isSuccess, string? userAgent = null, string? errorMessage = null, Guid? userId = null)
    {
        return new AccessLog(
            "LOGIN",
            ipAddress,
            tenantId,
            isSuccess,
            userId,
            username,
            userAgent: userAgent,
            errorMessage: errorMessage);
    }

    public static AccessLog CreateLogout(Guid userId, string username, string ipAddress, Guid tenantId, string? userAgent = null)
    {
        return new AccessLog(
            "LOGOUT",
            ipAddress,
            tenantId,
            true,
            userId,
            username,
            userAgent: userAgent);
    }

    public static AccessLog CreatePermissionChange(Guid adminUserId, string adminUsername, string action, string resource, string ipAddress, Guid tenantId, string? userAgent = null, string? additionalData = null)
    {
        return new AccessLog(
            action,
            ipAddress,
            tenantId,
            true,
            adminUserId,
            adminUsername,
            resource,
            userAgent,
            additionalData: additionalData);
    }
}