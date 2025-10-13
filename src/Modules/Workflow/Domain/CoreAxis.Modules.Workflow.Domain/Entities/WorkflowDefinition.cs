using System;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowDefinition
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<WorkflowDefinitionVersion> Versions { get; set; } = new List<WorkflowDefinitionVersion>();
}