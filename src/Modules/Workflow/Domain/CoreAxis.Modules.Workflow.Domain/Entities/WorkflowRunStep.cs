using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowRunStep : EntityBase
{
    public Guid WorkflowRunId { get; set; }
    public string StepId { get; set; } = null!;
    public string StepType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Attempts { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Error { get; set; }
    public string? LogJson { get; set; }
    public string? ExecutionKey { get; set; }
    
    public WorkflowRun WorkflowRun { get; set; } = null!;
}