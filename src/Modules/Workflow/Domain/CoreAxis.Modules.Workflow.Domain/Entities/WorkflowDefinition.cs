using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowDefinition : EntityBase
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string TenantId { get; set; } = "default";

    public ICollection<WorkflowDefinitionVersion> Versions { get; set; } = new List<WorkflowDefinitionVersion>();
}