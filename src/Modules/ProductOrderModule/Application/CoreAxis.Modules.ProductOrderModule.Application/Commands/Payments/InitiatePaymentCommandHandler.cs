using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Payments;

public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, Result<string>>
{
    private readonly IQuoteRepository _quoteRepository;

    public InitiatePaymentCommandHandler(IQuoteRepository quoteRepository)
    {
        _quoteRepository = quoteRepository;
    }

    public async Task<Result<string>> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId);
        if (quote == null)
            return Result<string>.Failure("Quote not found");

        // Mock Payment Gateway URL
        return Result<string>.Success($"https://mock-payment-gateway.com/pay?quoteId={quote.Id}&amount={quote.FinalPremium}");
    }
}
