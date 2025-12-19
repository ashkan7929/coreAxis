using CoreAxis.Modules.Workflow.Domain.Entities;

namespace CoreAxis.Modules.Workflow.Application.DTOs;

public class WorkflowRunDto
{
    public Guid Id { get; set; }
    public string DefinitionCode { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string Status { get; set; } = null!;
    public string ContextJson { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
