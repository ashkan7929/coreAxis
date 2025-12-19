using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowSignal : EntityBase
{
    public Guid WorkflowRunId { get; set; }
    public string Name { get; set; } = null!;
    public string? PayloadJson { get; set; }
    public DateTime? HandledAt { get; set; }
    
    public WorkflowRun WorkflowRun { get; set; } = null!;
}
