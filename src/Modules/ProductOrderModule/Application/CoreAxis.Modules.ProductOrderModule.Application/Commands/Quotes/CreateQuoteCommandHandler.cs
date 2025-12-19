using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;
using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.SharedKernel;
using MediatR;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Quotes;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, Result<QuoteResponseDto>>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IProductFlowResolver _flowResolver;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuoteCommandHandler(
        IQuoteRepository quoteRepository,
        IProductFlowResolver flowResolver,
        IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _flowResolver = flowResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<QuoteResponseDto>> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var assetCode = AssetCode.Create(request.AssetCode);
        
        // 1. Resolve Flow
        IQuoteFlow flow;
        try
        {
            flow = _flowResolver.ResolveQuoteFlow(assetCode);
        }
        catch (NotImplementedException ex)
        {
            return Result<QuoteResponseDto>.Failure(ex.Message);
        }

        // 2. Execute Flow (Calculate)
        var quoteResponse = await flow.CalculateQuoteAsync(request.ApplicationData, cancellationToken);

        // 3. Create Quote Entity
        var quote = Quote.Create(
            assetCode, 
            request.ApplicationData, 
            DateTime.UtcNow.AddDays(7)); // 7 days expiration

        quote.MarkAsReady(
            quoteResponse.FinalPremium, 
            JsonSerializer.Serialize(quoteResponse.Blocks)); // Persist blocks

        // 4. Save
        await _quoteRepository.AddAsync(quote);
        await _unitOfWork.SaveChangesAsync();

        // 5. Update Response with ID
        quoteResponse.QuoteId = quote.Id;

        return Result<QuoteResponseDto>.Success(quoteResponse);
    }
}
