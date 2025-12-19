using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowDefinitionVersion : EntityBase
{
    public Guid WorkflowDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public VersionStatus Status { get; set; }
    public string DslJson { get; set; } = null!;
    public string? Changelog { get; set; }
    public DateTime? PublishedAt { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }
}