namespace CoreAxis.SharedKernel.Ports;

public interface IWorkflowClient
{
    Task<WorkflowResult> StartAsync(string definitionId, object context, int? version = null, CancellationToken cancellationToken = default);
    Task<WorkflowResult> SignalAsync(string eventName, object payload, CancellationToken cancellationToken = default);
    Task<WorkflowResult> GetWorkflowStatusAsync(Guid workflowId, CancellationToken cancellationToken = default);
}

public class WorkflowResult
{
    public Guid WorkflowId { get; }
    public string Status { get; }
    public object? Result { get; }
    public string? Error { get; }
    public DateTime Timestamp { get; }

    public WorkflowResult(Guid workflowId, string status, object? result = null, string? error = null)
    {
        WorkflowId = workflowId;
        Status = status;
        Result = result;
        Error = error;
        Timestamp = DateTime.UtcNow;
    }

    public bool IsSuccess => string.IsNullOrEmpty(Error);
    public bool IsCompleted => Status == "Completed";
    public bool IsFailed => Status == "Failed";
}