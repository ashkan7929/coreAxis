using System.Text.Json.Serialization;

namespace CoreAxis.Modules.Workflow.Application.DTOs.DSL;

public class WorkflowDsl
{
    [JsonPropertyName("startAt")]
    public string StartAt { get; set; } = null!;

    [JsonPropertyName("steps")]
    public List<StepDsl> Steps { get; set; } = new();

    [JsonPropertyName("inputs")]
    public Dictionary<string, object>? Inputs { get; set; }
}

public class StepDsl
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("transitions")]
    public List<TransitionDsl>? Transitions { get; set; }

    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }

    [JsonPropertyName("compensation")]
    public List<CompensationActionDsl>? Compensation { get; set; }
}

public class CompensationActionDsl
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }
}

public class TransitionDsl
{
    [JsonPropertyName("to")]
    public string To { get; set; } = null!;

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}
