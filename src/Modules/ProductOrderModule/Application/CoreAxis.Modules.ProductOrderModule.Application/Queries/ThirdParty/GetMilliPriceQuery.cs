using CoreAxis.Modules.ProductOrderModule.Application.DTOs.ThirdParty;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Queries.ThirdParty;

public record GetMilliPriceQuery : IRequest<MilliPriceResponseDto>;