using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;

namespace CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;

public interface IQuoteFlow
{
    Task<QuoteResponseDto> CalculateQuoteAsync(string applicationData, CancellationToken cancellationToken);
}
