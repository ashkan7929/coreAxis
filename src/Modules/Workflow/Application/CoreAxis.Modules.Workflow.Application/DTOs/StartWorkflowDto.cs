namespace CoreAxis.Modules.Workflow.Application.DTOs;

public class StartWorkflowDto
{
    public string DefinitionCode { get; set; } = null!;
    public int? VersionNumber { get; set; }
    public string ContextJson { get; set; } = "{}";
    public string? CorrelationId { get; set; }
}
