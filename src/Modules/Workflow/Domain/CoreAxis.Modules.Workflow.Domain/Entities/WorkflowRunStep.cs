using System;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowRunStep
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string StepKey { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Attempt { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public string? Error { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}