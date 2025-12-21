using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain;

public class WebServiceCallLog : EntityBase
{

    public Guid WebServiceId { get; private set; }
    public Guid MethodId { get; private set; }
    public string? CorrelationId { get; private set; }
    public Guid? WorkflowRunId { get; private set; }
    public string? StepId { get; private set; }
    public string? RequestDump { get; private set; }
    public string? ResponseDump { get; private set; }
    public int? StatusCode { get; private set; }
    public long LatencyMs { get; private set; }
    public bool Succeeded { get; private set; }
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public WebService WebService { get; private set; } = null!;
    public WebServiceMethod Method { get; private set; } = null!;

    private WebServiceCallLog() { } // For EF

    public WebServiceCallLog(Guid webServiceId, Guid methodId, string? correlationId = null, Guid? workflowRunId = null, string? stepId = null)
    {
        Id = Guid.NewGuid();
        WebServiceId = webServiceId;
        MethodId = methodId;
        CorrelationId = correlationId;
        WorkflowRunId = workflowRunId;
        StepId = stepId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetRequest(string requestDump)
    {
        // Limit request dump size to prevent database bloat
        RequestDump = requestDump.Length > 10000 ? requestDump[..10000] + "..." : requestDump;
    }

    public void SetResponse(string responseDump, int statusCode, long latencyMs, bool succeeded, string? error = null)
    {
        // Limit response dump size to prevent database bloat
        ResponseDump = responseDump.Length > 10000 ? responseDump[..10000] + "..." : responseDump;
        StatusCode = statusCode;
        LatencyMs = latencyMs;
        Succeeded = succeeded;
        Error = error;
    }

    public void SetError(string error, long latencyMs)
    {
        Error = error;
        LatencyMs = latencyMs;
        Succeeded = false;
    }
}