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
    public string Name { get; private set; } = string.Empty;
    public SecurityType Type { get; private set; }
    public string ConfigJson { get; private set; } = string.Empty;
    public string? RotationPolicy { get; private set; }
    public bool IsActive { get; private set; } = true; // Added IsActive as it was used in repository
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<WebService> WebServices { get; private set; } = new List<WebService>();

    private SecurityProfile() { } // For EF

    public SecurityProfile(string name, SecurityType type, string configJson, string? rotationPolicy = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        ConfigJson = configJson;
        RotationPolicy = rotationPolicy;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, SecurityType type, string configJson, string? rotationPolicy = null)
    {
        Name = name;
        Type = type;
        ConfigJson = configJson;
        RotationPolicy = rotationPolicy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
