using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Payments;

public record InitiatePaymentCommand(Guid QuoteId) : IRequest<Result<string>>;
