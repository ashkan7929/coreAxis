using System;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowRun
{
    public Guid Id { get; set; }
    public Guid DefinitionId { get; set; }
    public int DefinitionVersionNumber { get; set; }
    public string Status { get; set; } = null!;
    public string InputContextJson { get; set; } = null!;
    public string? OutputContextJson { get; set; }
    public string CorrelationId { get; set; } = null!;
    public string? InitiatedBy { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? LastError { get; set; }

    public ICollection<WorkflowRunStep> Steps { get; set; } = new List<WorkflowRunStep>();
}