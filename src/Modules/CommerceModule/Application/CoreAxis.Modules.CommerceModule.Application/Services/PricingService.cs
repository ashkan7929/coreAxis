using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.CommerceModule.Application.Services;

/// <summary>
/// Service for calculating pricing and applying discounts to orders.
/// </summary>
public class PricingService : IPricingService
{
    private readonly ICommerceDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        ICommerceDbContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PricingService> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Applies all applicable discounts to an order snapshot.
    /// </summary>
    public async Task<PricingResult> ApplyDiscountsAsync(
        OrderSnapshot orderSnapshot,
        List<string>? couponCodes = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pricingContext = new PricingContext
            {
                OrderSnapshot = orderSnapshot,
                CouponCodes = couponCodes ?? new List<string>(),
                CorrelationId = correlationId,
                CalculatedAt = DateTime.UtcNow
            };

            // Get applicable discount rules
            var discountRules = await GetApplicableDiscountRulesAsync(
                pricingContext, cancellationToken);

            // Validate and process coupons
            var validCoupons = await ValidateAndProcessCouponsAsync(
                pricingContext, cancellationToken);

            // Calculate base pricing
            var basePricing = CalculateBasePricing(orderSnapshot);
            pricingContext.BasePricing = basePricing;

            // Apply automatic discounts (rules)
            var automaticDiscounts = await ApplyAutomaticDiscountsAsync(
                pricingContext, discountRules, cancellationToken);

            // Apply coupon discounts
            var couponDiscounts = await ApplyCouponDiscountsAsync(
                pricingContext, validCoupons, cancellationToken);

            // Combine and optimize discounts
            var finalDiscounts = OptimizeDiscounts(
                automaticDiscounts.Concat(couponDiscounts).ToList(),
                pricingContext);

            // Calculate final pricing
            var finalPricing = CalculateFinalPricing(
                basePricing, finalDiscounts, pricingContext);

            var result = new PricingResult
            {
                Success = true,
                BasePricing = basePricing,
                AppliedDiscounts = finalDiscounts,
                FinalPricing = finalPricing,
                ValidCoupons = validCoupons,
                CalculatedAt = pricingContext.CalculatedAt,
                CorrelationId = correlationId
            };

            // Dispatch pricing calculated event
            await _eventDispatcher.DispatchAsync(
                new OrderPricingCalculatedEvent(
                    orderSnapshot.OrderId,
                    basePricing.SubtotalAmount,
                    finalPricing.SubtotalAmount,
                    finalPricing.TotalDiscountAmount,
                    finalPricing.TaxAmount,
                    finalPricing.TotalAmount,
                    finalDiscounts.Select(d => new AppliedDiscountInfo(
                        d.DiscountId,
                        d.DiscountName,
                        d.DiscountType,
                        d.DiscountAmount,
                        d.CouponCode)).ToList(),
                    pricingContext.CalculatedAt,
                    correlationId),
                cancellationToken);

            _logger.LogInformation(
                "Successfully calculated pricing for order {OrderId}. Base: {BaseAmount}, Final: {FinalAmount}, Discount: {DiscountAmount}",
                orderSnapshot.OrderId, basePricing.TotalAmount, finalPricing.TotalAmount, finalPricing.TotalDiscountAmount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to calculate pricing for order {OrderId}", orderSnapshot.OrderId);
            
            return new PricingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CalculatedAt = DateTime.UtcNow,
                CorrelationId = correlationId
            };
        }
    }

    /// <summary>
    /// Gets applicable discount rules based on order context.
    /// </summary>
    private async Task<List<DiscountRule>> GetApplicableDiscountRulesAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        
        var rules = await _context.DiscountRules
            .Where(r => 
                r.IsActive &&
                r.StartDate <= now &&
                r.EndDate >= now &&
                r.DiscountType != DiscountType.Coupon) // Exclude coupon-based discounts
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        var applicableRules = new List<DiscountRule>();

        foreach (var rule in rules)
        {
            if (await IsRuleApplicableAsync(rule, context, cancellationToken))
            {
                applicableRules.Add(rule);
            }
        }

        return applicableRules;
    }

    /// <summary>
    /// Validates and processes coupon codes.
    /// </summary>
    private async Task<List<ValidCoupon>> ValidateAndProcessCouponsAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        var validCoupons = new List<ValidCoupon>();

        foreach (var couponCode in context.CouponCodes)
        {
            var coupon = await _context.DiscountRules
                .FirstOrDefaultAsync(r => 
                    r.CouponCode == couponCode &&
                    r.IsActive &&
                    r.DiscountType == DiscountType.Coupon,
                    cancellationToken);

            if (coupon == null)
            {
                _logger.LogWarning("Invalid coupon code: {CouponCode}", couponCode);
                continue;
            }

            var validation = await ValidateCouponAsync(coupon, context, cancellationToken);
            if (validation.IsValid)
            {
                validCoupons.Add(new ValidCoupon
                {
                    CouponCode = couponCode,
                    DiscountRule = coupon,
                    ValidationResult = validation
                });
            }
            else
            {
                _logger.LogWarning(
                    "Coupon validation failed for {CouponCode}: {Reason}", 
                    couponCode, validation.ErrorMessage);
            }
        }

        return validCoupons;
    }

    /// <summary>
    /// Validates a specific coupon against order context.
    /// </summary>
    private async Task<CouponValidationResult> ValidateCouponAsync(
        DiscountRule coupon,
        PricingContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Check date validity
        if (coupon.StartDate > now || coupon.EndDate < now)
        {
            return CouponValidationResult.Invalid("Coupon is not valid for current date");
        }

        // Check usage limits
        if (coupon.UsageLimit.HasValue)
        {
            var usageCount = await _context.CouponRedemptions
                .CountAsync(r => r.CouponCode == coupon.CouponCode, cancellationToken);

            if (usageCount >= coupon.UsageLimit.Value)
            {
                return CouponValidationResult.Invalid("Coupon usage limit exceeded");
            }
        }

        // Check customer usage limits
        if (coupon.UsageLimitPerCustomer.HasValue && context.OrderSnapshot.CustomerId.HasValue)
        {
            var customerUsageCount = await _context.CouponRedemptions
                .CountAsync(r => 
                    r.CouponCode == coupon.CouponCode &&
                    r.CustomerId == context.OrderSnapshot.CustomerId.Value,
                    cancellationToken);

            if (customerUsageCount >= coupon.UsageLimitPerCustomer.Value)
            {
                return CouponValidationResult.Invalid("Customer usage limit exceeded for this coupon");
            }
        }

        // Check minimum order amount
        if (coupon.MinimumOrderAmount.HasValue)
        {
            var orderTotal = context.BasePricing?.SubtotalAmount ?? 
                           context.OrderSnapshot.LineItems.Sum(li => li.Quantity * li.UnitPrice);

            if (orderTotal < coupon.MinimumOrderAmount.Value)
            {
                return CouponValidationResult.Invalid(
                    $"Minimum order amount of {coupon.MinimumOrderAmount.Value:C} required");
            }
        }

        // Check rule-specific conditions
        if (!await IsRuleApplicableAsync(coupon, context, cancellationToken))
        {
            return CouponValidationResult.Invalid("Coupon conditions not met");
        }

        return CouponValidationResult.Valid();
    }

    /// <summary>
    /// Checks if a discount rule is applicable to the current context.
    /// </summary>
    private async Task<bool> IsRuleApplicableAsync(
        DiscountRule rule,
        PricingContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rule.ConditionsJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<DiscountConditions>(rule.ConditionsJson);
            if (conditions == null)
                return true;

            // Check product conditions
            if (conditions.ProductIds?.Any() == true)
            {
                var hasMatchingProduct = context.OrderSnapshot.LineItems
                    .Any(li => conditions.ProductIds.Contains(li.ProductId));
                
                if (!hasMatchingProduct)
                    return false;
            }

            // Check category conditions
            if (conditions.CategoryIds?.Any() == true)
            {
                var productIds = context.OrderSnapshot.LineItems.Select(li => li.ProductId).ToList();
                var hasMatchingCategory = await _context.Products
                    .AnyAsync(p => 
                        productIds.Contains(p.Id) &&
                        conditions.CategoryIds.Contains(p.CategoryId),
                        cancellationToken);
                
                if (!hasMatchingCategory)
                    return false;
            }

            // Check customer group conditions
            if (conditions.CustomerGroupIds?.Any() == true && context.OrderSnapshot.CustomerId.HasValue)
            {
                var hasMatchingGroup = await _context.CustomerGroups
                    .AnyAsync(cg => 
                        conditions.CustomerGroupIds.Contains(cg.Id) &&
                        cg.CustomerId == context.OrderSnapshot.CustomerId.Value,
                        cancellationToken);
                
                if (!hasMatchingGroup)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Failed to parse discount rule conditions for rule {RuleId}", rule.Id);
            return false;
        }
    }

    /// <summary>
    /// Calculates base pricing without any discounts.
    /// </summary>
    private BasePricing CalculateBasePricing(OrderSnapshot orderSnapshot)
    {
        var subtotal = orderSnapshot.LineItems.Sum(li => li.Quantity * li.UnitPrice);
        var taxAmount = orderSnapshot.LineItems.Sum(li => li.TaxAmount);
        var shippingAmount = orderSnapshot.ShippingAmount;
        var total = subtotal + taxAmount + shippingAmount;

        return new BasePricing
        {
            SubtotalAmount = subtotal,
            TaxAmount = taxAmount,
            ShippingAmount = shippingAmount,
            TotalAmount = total,
            LineItemPricing = orderSnapshot.LineItems.Select(li => new LineItemPricing
            {
                LineItemId = li.Id,
                ProductId = li.ProductId,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                LineTotal = li.Quantity * li.UnitPrice,
                TaxAmount = li.TaxAmount
            }).ToList()
        };
    }

    /// <summary>
    /// Applies automatic discount rules.
    /// </summary>
    private async Task<List<AppliedDiscount>> ApplyAutomaticDiscountsAsync(
        PricingContext context,
        List<DiscountRule> rules,
        CancellationToken cancellationToken)
    {
        var appliedDiscounts = new List<AppliedDiscount>();

        foreach (var rule in rules)
        {
            var discount = CalculateDiscount(rule, context);
            if (discount != null && discount.DiscountAmount > 0)
            {
                appliedDiscounts.Add(discount);
            }
        }

        return appliedDiscounts;
    }

    /// <summary>
    /// Applies coupon-based discounts.
    /// </summary>
    private async Task<List<AppliedDiscount>> ApplyCouponDiscountsAsync(
        PricingContext context,
        List<ValidCoupon> validCoupons,
        CancellationToken cancellationToken)
    {
        var appliedDiscounts = new List<AppliedDiscount>();

        foreach (var coupon in validCoupons)
        {
            var discount = CalculateDiscount(coupon.DiscountRule, context);
            if (discount != null && discount.DiscountAmount > 0)
            {
                discount.CouponCode = coupon.CouponCode;
                appliedDiscounts.Add(discount);

                // Record coupon redemption
                await RecordCouponRedemptionAsync(coupon, context, discount, cancellationToken);
            }
        }

        return appliedDiscounts;
    }

    /// <summary>
    /// Calculates discount amount for a specific rule.
    /// </summary>
    private AppliedDiscount? CalculateDiscount(DiscountRule rule, PricingContext context)
    {
        var baseAmount = GetDiscountBaseAmount(rule, context);
        if (baseAmount <= 0)
            return null;

        decimal discountAmount = rule.DiscountType switch
        {
            DiscountType.Percentage => baseAmount * (rule.DiscountValue / 100m),
            DiscountType.FixedAmount => Math.Min(rule.DiscountValue, baseAmount),
            DiscountType.FreeShipping => context.OrderSnapshot.ShippingAmount,
            _ => 0
        };

        // Apply maximum discount limit
        if (rule.MaxDiscountAmount.HasValue)
        {
            discountAmount = Math.Min(discountAmount, rule.MaxDiscountAmount.Value);
        }

        return new AppliedDiscount
        {
            DiscountId = rule.Id,
            DiscountName = rule.Name,
            DiscountType = rule.DiscountType,
            DiscountValue = rule.DiscountValue,
            DiscountAmount = discountAmount,
            AppliedToAmount = baseAmount,
            Priority = rule.Priority
        };
    }

    /// <summary>
    /// Gets the base amount for discount calculation.
    /// </summary>
    private decimal GetDiscountBaseAmount(DiscountRule rule, PricingContext context)
    {
        return rule.ApplyTo switch
        {
            DiscountApplyTo.Order => context.BasePricing?.SubtotalAmount ?? 0,
            DiscountApplyTo.Shipping => context.OrderSnapshot.ShippingAmount,
            DiscountApplyTo.SpecificProducts => GetSpecificProductsAmount(rule, context),
            _ => context.BasePricing?.SubtotalAmount ?? 0
        };
    }

    /// <summary>
    /// Gets amount for specific products based on rule conditions.
    /// </summary>
    private decimal GetSpecificProductsAmount(DiscountRule rule, PricingContext context)
    {
        if (string.IsNullOrEmpty(rule.ConditionsJson))
            return 0;

        try
        {
            var conditions = JsonSerializer.Deserialize<DiscountConditions>(rule.ConditionsJson);
            if (conditions?.ProductIds?.Any() != true)
                return 0;

            return context.OrderSnapshot.LineItems
                .Where(li => conditions.ProductIds.Contains(li.ProductId))
                .Sum(li => li.Quantity * li.UnitPrice);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Optimizes discounts to prevent over-discounting and conflicts.
    /// </summary>
    private List<AppliedDiscount> OptimizeDiscounts(
        List<AppliedDiscount> discounts,
        PricingContext context)
    {
        // Sort by priority (lower number = higher priority)
        var sortedDiscounts = discounts.OrderBy(d => d.Priority).ToList();
        var optimizedDiscounts = new List<AppliedDiscount>();
        var totalDiscountApplied = 0m;
        var maxTotalDiscount = context.BasePricing?.TotalAmount ?? 0;

        foreach (var discount in sortedDiscounts)
        {
            var remainingDiscountCapacity = maxTotalDiscount - totalDiscountApplied;
            if (remainingDiscountCapacity <= 0)
                break;

            var adjustedDiscount = discount;
            if (discount.DiscountAmount > remainingDiscountCapacity)
            {
                adjustedDiscount = discount with { DiscountAmount = remainingDiscountCapacity };
            }

            optimizedDiscounts.Add(adjustedDiscount);
            totalDiscountApplied += adjustedDiscount.DiscountAmount;
        }

        return optimizedDiscounts;
    }

    /// <summary>
    /// Calculates final pricing after applying all discounts.
    /// </summary>
    private FinalPricing CalculateFinalPricing(
        BasePricing basePricing,
        List<AppliedDiscount> discounts,
        PricingContext context)
    {
        var totalDiscountAmount = discounts.Sum(d => d.DiscountAmount);
        var discountedSubtotal = basePricing.SubtotalAmount - totalDiscountAmount;
        var finalTotal = discountedSubtotal + basePricing.TaxAmount + basePricing.ShippingAmount;

        return new FinalPricing
        {
            SubtotalAmount = discountedSubtotal,
            TotalDiscountAmount = totalDiscountAmount,
            TaxAmount = basePricing.TaxAmount,
            ShippingAmount = basePricing.ShippingAmount,
            TotalAmount = finalTotal,
            AppliedDiscounts = discounts
        };
    }

    /// <summary>
    /// Records coupon redemption for tracking purposes.
    /// </summary>
    private async Task RecordCouponRedemptionAsync(
        ValidCoupon coupon,
        PricingContext context,
        AppliedDiscount discount,
        CancellationToken cancellationToken)
    {
        var redemption = new CouponRedemption
        {
            Id = Guid.NewGuid(),
            CouponCode = coupon.CouponCode,
            DiscountRuleId = coupon.DiscountRule.Id,
            OrderId = context.OrderSnapshot.OrderId,
            CustomerId = context.OrderSnapshot.CustomerId,
            DiscountAmount = discount.DiscountAmount,
            RedeemedAt = context.CalculatedAt,
            CorrelationId = context.CorrelationId
        };

        await _context.CouponRedemptions.AddAsync(redemption, cancellationToken);
    }
}



/// <summary>
/// Context for pricing calculations.
/// </summary>
public class PricingContext
{
    public OrderSnapshot OrderSnapshot { get; set; } = null!;
    public List<string> CouponCodes { get; set; } = new();
    public BasePricing? BasePricing { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string? CorrelationId { get; set; }
}