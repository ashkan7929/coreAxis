using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Coupons;

public record GetCouponRedemptionsQuery(
    Guid? UserId = null,
    Guid? OrderId = null,
    string? CouponCode = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<(List<CouponRedemptionDto> Redemptions, int TotalCount)>;

public class GetCouponRedemptionsQueryHandler : IRequestHandler<GetCouponRedemptionsQuery, (List<CouponRedemptionDto> Redemptions, int TotalCount)>
{
    private readonly ICouponRepository _couponRepository;
    private readonly ILogger<GetCouponRedemptionsQueryHandler> _logger;

    public GetCouponRedemptionsQueryHandler(
        ICouponRepository couponRepository,
        ILogger<GetCouponRedemptionsQueryHandler> logger)
    {
        _couponRepository = couponRepository;
        _logger = logger;
    }

    public async Task<(List<CouponRedemptionDto> Redemptions, int TotalCount)> Handle(GetCouponRedemptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (redemptions, totalCount) = await _couponRepository.GetCouponRedemptionsAsync(
                request.UserId,
                request.OrderId,
                request.CouponCode,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize);

            var redemptionDtos = redemptions.Select(redemption => new CouponRedemptionDto
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
            }).ToList();

            _logger.LogInformation("Retrieved {Count} coupon redemptions out of {TotalCount} total redemptions", 
                redemptionDtos.Count, totalCount);

            return (redemptionDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving coupon redemptions");
            throw;
        }
    }
}