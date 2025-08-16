using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain;

public class WebServiceMethod : EntityBase
{
    public Guid WebServiceId { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = string.Empty;
    public string? RequestSchema { get; private set; }
    public string? ResponseSchema { get; private set; }
    public int TimeoutMs { get; private set; }
    public string? RetryPolicyJson { get; private set; }
    public string? CircuitPolicyJson { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public WebService WebService { get; private set; } = null!;
    public ICollection<WebServiceParam> Parameters { get; private set; } = new List<WebServiceParam>();
    public ICollection<WebServiceCallLog> CallLogs { get; private set; } = new List<WebServiceCallLog>();

    private WebServiceMethod() { } // For EF

    public WebServiceMethod(Guid webServiceId, string path, string httpMethod, 
                           int timeoutMs = 30000, string? requestSchema = null, 
                           string? responseSchema = null, string? retryPolicyJson = null, 
                           string? circuitPolicyJson = null)
    {
        Id = Guid.NewGuid();
        WebServiceId = webServiceId;
        Path = path;
        HttpMethod = httpMethod.ToUpperInvariant();
        TimeoutMs = timeoutMs;
        RequestSchema = requestSchema;
        ResponseSchema = responseSchema;
        RetryPolicyJson = retryPolicyJson;
        CircuitPolicyJson = circuitPolicyJson;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string path, string httpMethod, int timeoutMs, 
                      string? requestSchema = null, string? responseSchema = null, 
                      string? retryPolicyJson = null, string? circuitPolicyJson = null)
    {
        Path = path;
        HttpMethod = httpMethod.ToUpperInvariant();
        TimeoutMs = timeoutMs;
        RequestSchema = requestSchema;
        ResponseSchema = responseSchema;
        RetryPolicyJson = retryPolicyJson;
        CircuitPolicyJson = circuitPolicyJson;
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