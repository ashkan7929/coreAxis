using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowDefinition : EntityBase, IMustHaveTenant
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string TenantId { get; set; } = null!; // Already exists, just implementing interface

    public ICollection<WorkflowDefinitionVersion> Versions { get; set; } = new List<WorkflowDefinitionVersion>();
}