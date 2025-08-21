using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

/// <summary>
/// Event raised when order pricing has been calculated.
/// </summary>
public class OrderPricingCalculatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public decimal OriginalAmount { get; }
    public decimal FinalAmount { get; }
    public decimal TotalDiscountAmount { get; }
    public decimal TaxAmount { get; }
    public decimal TotalAmount { get; }
    public List<AppliedDiscountInfo> AppliedDiscounts { get; }
    public DateTime CalculatedAt { get; }
    public string? CorrelationId { get; }

    public OrderPricingCalculatedEvent(Guid orderid, decimal originalamount, decimal finalamount, decimal totaldiscountamount, decimal taxamount, decimal totalamount, List<AppliedDiscountInfo> applieddiscounts, DateTime calculatedat, string? correlationId = null)
    {
        OrderId = orderid;
        OriginalAmount = originalamount;
        FinalAmount = finalamount;
        TotalDiscountAmount = totaldiscountamount;
        TaxAmount = taxamount;
        TotalAmount = totalamount;
        AppliedDiscounts = applieddiscounts;
        CalculatedAt = calculatedat;
        CorrelationId = correlationId;
    }
}





/// <summary>
/// Event raised when coupon validation fails.
/// </summary>
public class CouponValidationFailedEvent : DomainEvent
{
    public string CouponCode { get; }
    public Guid OrderId { get; }
    public Guid? CustomerId { get; }
    public string ValidationError { get; }
    public DateTime FailedAt { get; }
    public string? CorrelationId { get; }

    public CouponValidationFailedEvent(string couponcode, Guid orderid, Guid? customerid, string validationerror, DateTime failedat, string? correlationId = null)
    {
        CouponCode = couponcode;
        OrderId = orderid;
        CustomerId = customerid;
        ValidationError = validationerror;
        FailedAt = failedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a discount rule reaches its usage limit.
/// </summary>
public class DiscountUsageLimitReachedEvent : DomainEvent
{
    public Guid DiscountRuleId { get; }
    public string DiscountName { get; }
    public string? CouponCode { get; }
    public int UsageLimit { get; }
    public DateTime ReachedAt { get; }
    public string? CorrelationId { get; }

    public DiscountUsageLimitReachedEvent(Guid discountruleid, string discountname, string? couponcode, int usagelimit, DateTime reachedat, string? correlationId = null)
    {
        DiscountRuleId = discountruleid;
        DiscountName = discountname;
        CouponCode = couponcode;
        UsageLimit = usagelimit;
        ReachedAt = reachedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when pricing calculation fails.
/// </summary>
public class PricingCalculationFailedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string ErrorMessage { get; }
    public List<string>? CouponCodes { get; }
    public DateTime FailedAt { get; }
    public string? CorrelationId { get; }

    public PricingCalculationFailedEvent(Guid orderid, string errormessage, List<string>? couponcodes, DateTime failedat, string? correlationId = null)
    {
        OrderId = orderid;
        ErrorMessage = errormessage;
        CouponCodes = couponcodes;
        FailedAt = failedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a discount conflict is detected.
/// </summary>
public class DiscountConflictDetectedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public List<ConflictingDiscountInfo> ConflictingDiscounts { get; }
    public string ResolutionStrategy { get; }
    public DateTime DetectedAt { get; }
    public string? CorrelationId { get; }

    public DiscountConflictDetectedEvent(Guid orderid, List<ConflictingDiscountInfo> conflictingdiscounts, string resolutionstrategy, DateTime detectedat, string? correlationId = null)
    {
        OrderId = orderid;
        ConflictingDiscounts = conflictingdiscounts;
        ResolutionStrategy = resolutionstrategy;
        DetectedAt = detectedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when maximum discount limit is exceeded.
/// </summary>
public class MaxDiscountLimitExceededEvent : DomainEvent
{
    public Guid OrderId { get; }
    public decimal RequestedDiscountAmount { get; }
    public decimal MaxAllowedDiscountAmount { get; }
    public decimal AppliedDiscountAmount { get; }
    public DateTime ExceededAt { get; }
    public string? CorrelationId { get; }

    public MaxDiscountLimitExceededEvent(Guid orderid, decimal requesteddiscountamount, decimal maxalloweddiscountamount, decimal applieddiscountamount, DateTime exceededat, string? correlationId = null)
    {
        OrderId = orderid;
        RequestedDiscountAmount = requesteddiscountamount;
        MaxAllowedDiscountAmount = maxalloweddiscountamount;
        AppliedDiscountAmount = applieddiscountamount;
        ExceededAt = exceededat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a promotional discount is automatically applied.
/// </summary>
public class PromotionalDiscountAppliedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid PromotionId { get; }
    public string PromotionName { get; }
    public DiscountType DiscountType { get; }
    public decimal DiscountAmount { get; }
    public string TriggerCondition { get; }
    public DateTime AppliedAt { get; }
    public string? CorrelationId { get; }

    public PromotionalDiscountAppliedEvent(Guid orderid, Guid promotionid, string promotionname, DiscountType discounttype, decimal discountamount, string triggercondition, DateTime appliedat, string? correlationId = null)
    {
        OrderId = orderid;
        PromotionId = promotionid;
        PromotionName = promotionname;
        DiscountType = discounttype;
        DiscountAmount = discountamount;
        TriggerCondition = triggercondition;
        AppliedAt = appliedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when bulk pricing is applied.
/// </summary>
public class BulkPricingAppliedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public List<BulkPricingInfo> BulkPricingRules { get; }
    public decimal TotalBulkDiscount { get; }
    public DateTime AppliedAt { get; }
    public string? CorrelationId { get; }

    public BulkPricingAppliedEvent(Guid orderid, List<BulkPricingInfo> bulkpricingrules, decimal totalbulkdiscount, DateTime appliedat, string? correlationId = null)
    {
        OrderId = orderid;
        BulkPricingRules = bulkpricingrules;
        TotalBulkDiscount = totalbulkdiscount;
        AppliedAt = appliedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Information about an applied discount.
/// </summary>
public class AppliedDiscountInfo(
    Guid DiscountId,
    string DiscountName,
    DiscountType DiscountType,
    decimal DiscountAmount,
    string? CouponCode = null
);

/// <summary>
/// Information about conflicting discounts.
/// </summary>
public class ConflictingDiscountInfo(
    Guid DiscountId,
    string DiscountName,
    DiscountType DiscountType,
    decimal DiscountAmount,
    int Priority,
    string ConflictReason
);

/// <summary>
/// Information about bulk pricing rules.
/// </summary>
public class BulkPricingInfo(
    Guid ProductId,
    string ProductName,
    int Quantity,
    int MinQuantityForDiscount,
    decimal OriginalUnitPrice,
    decimal BulkUnitPrice,
    decimal TotalDiscount
);