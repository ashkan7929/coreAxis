using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class HumanTaskRequested : IntegrationEvent
{
    public Guid WorkflowRunId { get; }
    public string StepId { get; }
    public string AssigneeType { get; }
    public string AssigneeId { get; }
    public string? PayloadJson { get; }
    public string? AllowedActionsJson { get; }
    public DateTime? DueAt { get; }

    public HumanTaskRequested(
        Guid workflowRunId, 
        string stepId, 
        string assigneeType, 
        string assigneeId, 
        string? payloadJson,
        string? allowedActionsJson,
        DateTime? dueAt,
        Guid correlationId)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId)
    {
        WorkflowRunId = workflowRunId;
        StepId = stepId;
        AssigneeType = assigneeType;
        AssigneeId = assigneeId;
        PayloadJson = payloadJson;
        AllowedActionsJson = allowedActionsJson;
        DueAt = dueAt;
    }
}

public class HumanTaskCompleted : IntegrationEvent
{
    public Guid WorkflowRunId { get; }
    public Guid TaskId { get; }
    public string Outcome { get; } // Approved, Rejected, Returned
    public string? PayloadJson { get; }
    public string? Comment { get; }

    public HumanTaskCompleted(
        Guid workflowRunId, 
        Guid taskId, 
        string outcome, 
        string? payloadJson, 
        string? comment,
        Guid correlationId)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId)
    {
        WorkflowRunId = workflowRunId;
        TaskId = taskId;
        Outcome = outcome;
        PayloadJson = payloadJson;
        Comment = comment;
    }
}
