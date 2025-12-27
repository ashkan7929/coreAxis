using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;

namespace CoreAxis.SharedKernel.Ports;

public interface IWorkflowRunner
{
    Task<WorkflowRunResult> RunAsync(
        string workflowCode,
        int workflowVersion,
        CoreAxisExecutionContext context,
        CancellationToken ct);
}

public sealed class WorkflowRunResult
{
    public CoreAxisExecutionContext Context { get; init; } = default!;
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public object? Output { get; init; }
    public string? OutputJson { get; init; }
}
