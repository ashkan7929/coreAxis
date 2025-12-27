using System.Text.Json.Serialization;

namespace CoreAxis.Modules.ProductBuilderModule.Api.DTOs;

public class ProductFormSchemaDto
{
    [JsonPropertyName("productKey")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("productVersion")]
    public string ProductVersion { get; set; } = string.Empty;

    [JsonPropertyName("formId")]
    public Guid FormId { get; set; }

    [JsonPropertyName("formVersion")]
    public int FormVersion { get; set; }

    [JsonPropertyName("schemaJson")]
    public object SchemaJson { get; set; } = default!;
}
