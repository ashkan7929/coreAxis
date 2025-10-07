using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;

namespace CoreAxis.Modules.CommerceModule.Api.Controllers;

/// <summary>
/// Controller for pricing calculation endpoints
/// </summary>
[ApiController]
[Route("api/v1/commerce/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;
    private readonly ILogger<PricingController> _logger;

    public PricingController(IPricingService pricingService, ILogger<PricingController> logger)
    {
        _pricingService = pricingService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates pricing and applies discounts for an order snapshot
    /// </summary>
    /// <param name="request">Order snapshot and optional coupon codes</param>
    /// <returns>Pricing result including base and final pricing</returns>
    [HttpPost("calculate")]
    [HasPermission("pricing", "calculate")]
    public async Task<ActionResult<PricingResultDto>> Calculate([FromBody] PricingCalculateRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var correlationId = request.CorrelationId ?? Request.Headers["X-Correlation-ID"].FirstOrDefault();
            _logger.LogInformation("Calculating pricing for order {OrderId} with correlationId {CorrelationId}", request.Order.OrderId, correlationId);

            var snapshot = MapToDomain(request.Order);

            var result = await _pricingService.ApplyDiscountsAsync(snapshot, request.CouponCodes, correlationId, cancellationToken);

            var dto = MapToDto(result);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument during pricing calculation");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during pricing calculation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pricing");
            return StatusCode(500, "An error occurred while calculating pricing");
        }
    }

    private static OrderSnapshot MapToDomain(OrderSnapshotDto dto)
    {
        return new OrderSnapshot
        {
            OrderId = dto.OrderId,
            CustomerId = dto.CustomerId,
            SubtotalAmount = dto.SubtotalAmount,
            Currency = dto.Currency,
            Metadata = dto.Metadata,
            Items = dto.Items.Select(i => new OrderItemSnapshot
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                CategoryIds = i.CategoryIds
            }).ToList()
        };
    }

    private static PricingResultDto MapToDto(PricingResult result)
    {
        var dto = new PricingResultDto
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CalculatedAt = result.CalculatedAt,
            CorrelationId = result.CorrelationId
        };

        if (result.BasePricing != null)
        {
            dto.BasePricing = new BasePricingDto
            {
                SubtotalAmount = result.BasePricing.SubtotalAmount,
                TaxAmount = result.BasePricing.TaxAmount,
                ShippingAmount = result.BasePricing.ShippingAmount,
                TotalAmount = result.BasePricing.TotalAmount,
                LineItemPricing = result.BasePricing.LineItemPricing.Select(lp => new LineItemPricingDto
                {
                    LineItemId = lp.LineItemId,
                    ProductId = lp.ProductId,
                    Quantity = lp.Quantity,
                    UnitPrice = lp.UnitPrice,
                    LineTotal = lp.LineTotal,
                    TaxAmount = lp.TaxAmount
                }).ToList()
            };
        }

        if (result.FinalPricing != null)
        {
            dto.FinalPricing = new FinalPricingDto
            {
                SubtotalAmount = result.FinalPricing.SubtotalAmount,
                TotalDiscountAmount = result.FinalPricing.TotalDiscountAmount,
                TaxAmount = result.FinalPricing.TaxAmount,
                ShippingAmount = result.FinalPricing.ShippingAmount,
                TotalAmount = result.FinalPricing.TotalAmount,
                AppliedDiscounts = result.FinalPricing.AppliedDiscounts.Select(MapAppliedDiscount).ToList()
            };
        }

        dto.AppliedDiscounts = result.AppliedDiscounts.Select(MapAppliedDiscount).ToList();
        dto.ValidCoupons = result.ValidCoupons.Select(vc => new ValidCouponDto
        {
            CouponCode = vc.CouponCode,
            IsValid = vc.ValidationResult.IsValid,
            ErrorMessage = vc.ValidationResult.ErrorMessage
        }).ToList();

        return dto;
    }

    private static AppliedDiscountDto MapAppliedDiscount(AppliedDiscount ad)
    {
        return new AppliedDiscountDto
        {
            DiscountId = ad.DiscountId,
            DiscountName = ad.DiscountName,
            DiscountType = ad.DiscountType.ToString(),
            DiscountValue = ad.DiscountValue,
            DiscountAmount = ad.DiscountAmount,
            AppliedToAmount = ad.AppliedToAmount,
            Priority = ad.Priority,
            CouponCode = ad.CouponCode
        };
    }
}