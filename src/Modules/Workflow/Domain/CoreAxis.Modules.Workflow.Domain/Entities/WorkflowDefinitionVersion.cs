using System;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class WorkflowDefinitionVersion
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsPublished { get; set; }
    public string DslJson { get; set; } = null!;
    public int SchemaVersion { get; set; }
    public string? Changelog { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public WorkflowDefinition? WorkflowDefinition { get; set; }
}