using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowTransition : EntityBase
{
    public Guid WorkflowRunId { get; set; }
    public string FromStepId { get; set; } = null!;
    public string ToStepId { get; set; } = null!;
    public string? Condition { get; set; }
    public bool Chosen { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public string? TraceJson { get; set; }
    
    public WorkflowRun WorkflowRun { get; set; } = null!;
}
