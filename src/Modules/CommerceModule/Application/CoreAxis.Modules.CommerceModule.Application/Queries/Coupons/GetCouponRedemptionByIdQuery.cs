using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Coupons;

public record GetCouponRedemptionByIdQuery(Guid Id) : IRequest<CouponRedemptionDto?>;

public class GetCouponRedemptionByIdQueryHandler : IRequestHandler<GetCouponRedemptionByIdQuery, CouponRedemptionDto?>
{
    private readonly ICouponRepository _couponRepository;
    private readonly ILogger<GetCouponRedemptionByIdQueryHandler> _logger;

    public GetCouponRedemptionByIdQueryHandler(
        ICouponRepository couponRepository,
        ILogger<GetCouponRedemptionByIdQueryHandler> logger)
    {
        _couponRepository = couponRepository;
        _logger = logger;
    }

    public async Task<CouponRedemptionDto?> Handle(GetCouponRedemptionByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var redemption = await _couponRepository.GetCouponRedemptionByIdAsync(request.Id);
            if (redemption == null)
            {
                _logger.LogWarning("Coupon redemption with ID {RedemptionId} not found", request.Id);
                return null;
            }

            var redemptionDto = new CouponRedemptionDto
            {
                Id = redemption.Id,
                UserId = redemption.UserId,
                OrderId = redemption.OrderId,
                CouponCode = redemption.CouponCode,
                DiscountAmount = redemption.DiscountAmount,
                Currency = redemption.Currency,
                RedeemedAt = redemption.RedeemedAt,
                Status = redemption.Status,
                CreatedAt = redemption.CreatedAt,
                UpdatedAt = redemption.UpdatedAt
            };

            _logger.LogInformation("Retrieved coupon redemption with ID: {RedemptionId}", request.Id);
            return redemptionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving coupon redemption with ID: {RedemptionId}", request.Id);
            throw;
        }
    }
}