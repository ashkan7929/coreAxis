namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;

public class CreateQuoteRequestDto
{
    public string AssetCode { get; set; } = string.Empty;
    public object ApplicationData { get; set; } = new object();
}
