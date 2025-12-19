using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.Workflow.Domain.Events;

public class WorkflowRunStartedDomainEvent : DomainEvent
{
    public Guid WorkflowRunId { get; }
    public string DefinitionCode { get; }
    public int VersionNumber { get; }
    public string ContextJson { get; }

    public WorkflowRunStartedDomainEvent(Guid workflowRunId, string definitionCode, int versionNumber, string contextJson)
    {
        WorkflowRunId = workflowRunId;
        DefinitionCode = definitionCode;
        VersionNumber = versionNumber;
        ContextJson = contextJson;
    }
}
