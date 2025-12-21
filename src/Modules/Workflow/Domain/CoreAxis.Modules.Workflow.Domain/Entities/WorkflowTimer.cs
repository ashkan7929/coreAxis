using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowTimer : EntityBase
{
    public Guid WorkflowRunId { get; set; }
    public string StepId { get; set; } = default!;
    public DateTime DueAt { get; set; }
    public string SignalName { get; set; } = default!;
    public string? PayloadJson { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processed, Cancelled
}
