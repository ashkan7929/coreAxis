using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;
using CoreAxis.SharedKernel;
using MediatR;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Payments;

public class PaymentCallbackCommandHandler : IRequestHandler<PaymentCallbackCommand, Result<bool>>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFanavaranConnector _fanavaranConnector;

    public PaymentCallbackCommandHandler(
        IQuoteRepository quoteRepository, 
        IUnitOfWork unitOfWork,
        IFanavaranConnector fanavaranConnector)
    {
        _quoteRepository = quoteRepository;
        _unitOfWork = unitOfWork;
        _fanavaranConnector = fanavaranConnector;
    }

    public async Task<Result<bool>> Handle(PaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        if (!request.Success)
            return Result<bool>.Failure("Payment failed");

        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId);
        if (quote == null)
            return Result<bool>.Failure("Quote not found");

        // 1. Mark Quote as Converted/Paid
        quote.MarkAsConverted();

        // 2. Trigger Post Payment Actions (Issue Policy)
        await _fanavaranConnector.IssuePolicyAsync(quote.ApplicationData, cancellationToken);
        
        // TODO: Emit SendToFinance event
        // _eventBus.Publish(new SendToFinanceEvent(quote.Id));

        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }
}
