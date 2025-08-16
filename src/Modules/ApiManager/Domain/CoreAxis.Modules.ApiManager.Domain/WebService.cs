using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain;

public class WebService : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? SecurityProfileId { get; private set; }
    public bool IsActive { get; private set; }
    public string? OwnerTenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public SecurityProfile? SecurityProfile { get; private set; }
    public ICollection<WebServiceMethod> Methods { get; private set; } = new List<WebServiceMethod>();
    public ICollection<WebServiceCallLog> CallLogs { get; private set; } = new List<WebServiceCallLog>();

    private WebService() { } // For EF

    public WebService(string name, string baseUrl, string? description = null, 
                     Guid? securityProfileId = null, string? ownerTenantId = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        BaseUrl = baseUrl;
        Description = description;
        SecurityProfileId = securityProfileId;
        IsActive = true;
        OwnerTenantId = ownerTenantId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string baseUrl, string? description = null, 
                      Guid? securityProfileId = null)
    {
        Name = name;
        BaseUrl = baseUrl;
        Description = description;
        SecurityProfileId = securityProfileId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}