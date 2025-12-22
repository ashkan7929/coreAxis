namespace CoreAxis.Modules.Workflow.Application.Services;

public interface IWorkflowExecutor
{
    Task ExecuteStepAsync(Guid workflowRunId, string stepId, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid workflowRunId, Dictionary<string, object> input, CancellationToken cancellationToken = default);
    Task SignalAsync(Guid workflowRunId, string signalName, Dictionary<string, object> payload, CancellationToken cancellationToken = default);
    Task SignalByCorrelationAsync(string correlationId, string signalName, Dictionary<string, object> payload, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid workflowRunId, string reason, CancellationToken cancellationToken = default);
}
