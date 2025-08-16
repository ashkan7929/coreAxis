using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain;

public enum ParameterLocation
{
    Query = 0,
    Header = 1,
    Route = 2,
    Body = 3
}

public class WebServiceParam : EntityBase
{

    public Guid MethodId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ParameterLocation Location { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public WebServiceMethod Method { get; private set; } = null!;

    private WebServiceParam() { } // For EF

    public WebServiceParam(Guid methodId, string name, ParameterLocation location, 
                          string type, bool isRequired = false, string? defaultValue = null)
    {
        Id = Guid.NewGuid();
        MethodId = methodId;
        Name = name;
        Location = location;
        Type = type;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, ParameterLocation location, string type, 
                      bool isRequired, string? defaultValue = null)
    {
        Name = name;
        Location = location;
        Type = type;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        UpdatedAt = DateTime.UtcNow;
    }
}