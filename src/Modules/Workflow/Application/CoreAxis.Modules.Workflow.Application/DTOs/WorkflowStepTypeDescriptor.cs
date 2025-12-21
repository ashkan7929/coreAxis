using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.DTOs;

public class WorkflowStepTypeDescriptor
{
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public string? Icon { get; set; }
    public JsonElement ConfigSchema { get; set; }
    public List<string> RequiredContextPaths { get; set; } = new();
    public List<string> ProducedContextPaths { get; set; } = new();
    public List<string> PauseSignalNames { get; set; } = new();
}
