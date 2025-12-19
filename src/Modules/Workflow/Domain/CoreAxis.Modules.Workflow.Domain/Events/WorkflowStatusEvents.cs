using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.Workflow.Domain.Events;

public class WorkflowPausedDomainEvent : DomainEvent
{
    public Guid WorkflowRunId { get; }
    public string Reason { get; }
    public string StepId { get; }

    public WorkflowPausedDomainEvent(Guid workflowRunId, string stepId, string reason = "Step execution paused")
    {
        WorkflowRunId = workflowRunId;
        StepId = stepId;
        Reason = reason;
    }
}

public class WorkflowResumedDomainEvent : DomainEvent
{
    public Guid WorkflowRunId { get; }
    public string SignalName { get; }

    public WorkflowResumedDomainEvent(Guid workflowRunId, string signalName)
    {
        WorkflowRunId = workflowRunId;
        SignalName = signalName;
    }
}

public class WorkflowCompletedDomainEvent : DomainEvent
{
    public Guid WorkflowRunId { get; }

    public WorkflowCompletedDomainEvent(Guid workflowRunId)
    {
        WorkflowRunId = workflowRunId;
    }
}

public class WorkflowFailedDomainEvent : DomainEvent
{
    public Guid WorkflowRunId { get; }
    public string Error { get; }

    public WorkflowFailedDomainEvent(Guid workflowRunId, string error)
    {
        WorkflowRunId = workflowRunId;
        Error = error;
    }
}

public class WorkflowCancelledDomainEvent : DomainEvent
{
    public Guid WorkflowRunId { get; }
    public string Reason { get; }

    public WorkflowCancelledDomainEvent(Guid workflowRunId, string reason)
    {
        WorkflowRunId = workflowRunId;
        Reason = reason;
    }
}
