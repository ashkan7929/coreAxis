using CoreAxis.SharedKernel;
using CoreAxis.Modules.Workflow.Domain.Events;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowRun : EntityBase
{
    public string WorkflowDefinitionCode { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string Status { get; set; } = null!;
    public string ContextJson { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    
    // Additional fields from EntityBase: Id, CreatedOn, LastModifiedOn

    public ICollection<WorkflowRunStep> Steps { get; set; } = new List<WorkflowRunStep>();

    public void Start()
    {
        AddDomainEvent(new WorkflowRunStartedDomainEvent(Id, WorkflowDefinitionCode, VersionNumber, ContextJson));
    }

    public void Pause(string stepId, string reason = "Step execution paused")
    {
        Status = "Paused";
        AddDomainEvent(new WorkflowPausedDomainEvent(Id, stepId, reason));
    }

    public void Resume(string signalName)
    {
        Status = "Running";
        AddDomainEvent(new WorkflowResumedDomainEvent(Id, signalName));
    }

    public void Complete()
    {
        Status = "Completed";
        AddDomainEvent(new WorkflowCompletedDomainEvent(Id));
    }

    public void Fail(string error)
    {
        Status = "Failed";
        AddDomainEvent(new WorkflowFailedDomainEvent(Id, error));
    }

    public void Cancel(string reason)
    {
        Status = "Cancelled";
        AddDomainEvent(new WorkflowCancelledDomainEvent(Id, reason));
    }
}