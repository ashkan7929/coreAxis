namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;

public class QuoteResponseDto
{
    public Guid QuoteId { get; set; }
    public decimal FinalPremium { get; set; }
    public List<UiBlock> Blocks { get; set; } = new();
    public object? RawVars { get; set; } // For debugging/internal use
}
