namespace CoreAxis.Modules.Workflow.Application.DTOs;

public class WorkflowRunHistoryDto
{
    public Guid RunId { get; set; }
    public List<WorkflowStepDto> Steps { get; set; } = new();
    public List<WorkflowTransitionDto> Transitions { get; set; } = new();
}

public class WorkflowStepDto
{
    public string StepId { get; set; } = null!;
    public string StepType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Error { get; set; }
}

public class WorkflowTransitionDto
{
    public string FromStepId { get; set; } = null!;
    public string ToStepId { get; set; } = null!;
    public DateTime EvaluatedAt { get; set; }
}
