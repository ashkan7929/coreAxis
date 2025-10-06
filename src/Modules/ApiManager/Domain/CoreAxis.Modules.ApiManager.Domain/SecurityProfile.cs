using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain;

public enum SecurityType
{
    None = 0,
    ApiKey = 1,
    OAuth2 = 2,
    HMAC = 3
}

public class SecurityProfile : EntityBase
{

    public SecurityType Type { get; private set; }
    public string ConfigJson { get; private set; } = string.Empty;
    public string? RotationPolicy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<WebService> WebServices { get; private set; } = new List<WebService>();

    private SecurityProfile() { } // For EF

    public SecurityProfile(SecurityType type, string configJson, string? rotationPolicy = null)
    {
        Id = Guid.NewGuid();
        Type = type;
        ConfigJson = configJson;
        RotationPolicy = rotationPolicy;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(SecurityType type, string configJson, string? rotationPolicy = null)
    {
        Type = type;
        ConfigJson = configJson;
        RotationPolicy = rotationPolicy;
        UpdatedAt = DateTime.UtcNow;
    }
}