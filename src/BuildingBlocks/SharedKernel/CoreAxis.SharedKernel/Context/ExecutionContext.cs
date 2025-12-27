using System.Text.Json.Serialization;

namespace CoreAxis.SharedKernel.Context;

public class ExecutionContext
{
    [JsonPropertyName("form")]
    public Dictionary<string, object> Form { get; set; } = new();

    [JsonPropertyName("vars")]
    public Dictionary<string, object> Vars { get; set; } = new();

    [JsonPropertyName("steps")]
    public Dictionary<string, StepContext> Steps { get; set; } = new();

    [JsonPropertyName("meta")]
    public ExecutionContextMeta Meta { get; set; } = new();
}

public class ExecutionContextMeta
{
    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("assetCode")]
    public string AssetCode { get; set; } = string.Empty;
}

public class StepContext
{
    [JsonPropertyName("response")]
    public object? Response { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
