using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Payments;

public record PaymentCallbackCommand(Guid QuoteId, bool Success, string TransactionId) : IRequest<Result<bool>>;
