using System.Text.Json.Serialization;

namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PriceBreakdownBlock), typeDiscriminator: "priceBreakdown")]
[JsonDerivedType(typeof(TableBlock), typeDiscriminator: "table")]
[JsonDerivedType(typeof(MessageBlock), typeDiscriminator: "message")]
public abstract class UiBlock
{
    public string Title { get; set; } = string.Empty;
}

public class PriceBreakdownBlock : UiBlock
{
    public List<PriceItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class PriceItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TableBlock : UiBlock
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public class MessageBlock : UiBlock
{
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // info, warning, error
}
