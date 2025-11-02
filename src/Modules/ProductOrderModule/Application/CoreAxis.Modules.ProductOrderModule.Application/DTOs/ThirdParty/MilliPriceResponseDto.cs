namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs.ThirdParty;

public class MilliPriceResponseDto
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public MilliPriceDataDto Data { get; set; } = null!;
}