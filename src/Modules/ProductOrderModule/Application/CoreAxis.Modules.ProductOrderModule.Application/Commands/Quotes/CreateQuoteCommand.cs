using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Quotes;

public record CreateQuoteCommand(string AssetCode, string ApplicationData) : IRequest<Result<QuoteResponseDto>>;
