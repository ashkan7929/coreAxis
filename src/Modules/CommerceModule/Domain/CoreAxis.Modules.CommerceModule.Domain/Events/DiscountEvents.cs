using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

/// <summary>
/// Event raised when a discount rule is created.
/// </summary>
public class DiscountRuleCreatedEvent : DomainEvent
{
    public Guid DiscountRuleId { get; }
    public string Name { get; }
    public string Scope { get; }
    public string Type { get; }
    public decimal Value { get; }
    public bool RequiresCoupon { get; }
    public string? CouponCode { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }
    public DateTime CreatedAt { get; }

    public DiscountRuleCreatedEvent(Guid discountRuleId, string name, string scope, string type, decimal value, bool requiresCoupon, string? couponCode, DateTime? startDate, DateTime? endDate, DateTime createdAt)
    {
        DiscountRuleId = discountRuleId;
        Name = name;
        Scope = scope;
        Type = type;
        Value = value;
        RequiresCoupon = requiresCoupon;
        CouponCode = couponCode;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = createdAt;
    }
}

/// <summary>
/// Event raised when a discount rule is updated.
/// </summary>
public class DiscountRuleUpdatedEvent : DomainEvent
{
    public Guid DiscountRuleId { get; }
    public string Name { get; }
    public bool IsActive { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }
    public DateTime UpdatedAt { get; }

    public DiscountRuleUpdatedEvent(Guid discountruleid, string name, bool isactive, DateTime? startdate, DateTime? enddate, DateTime updatedat)
    {
        DiscountRuleId = discountruleid;
        Name = name;
        IsActive = isactive;
        StartDate = startdate;
        EndDate = enddate;
        UpdatedAt = updatedat;
    }
}

/// <summary>
/// Event raised when a discount rule is deactivated.
/// </summary>
public class DiscountRuleDeactivatedEvent : DomainEvent
{
    public Guid DiscountRuleId { get; }
    public string Name { get; }
    public string Reason { get; }
    public DateTime DeactivatedAt { get; }

    public DiscountRuleDeactivatedEvent(Guid discountruleid, string name, string reason, DateTime deactivatedat)
    {
        DiscountRuleId = discountruleid;
        Name = name;
        Reason = reason;
        DeactivatedAt = deactivatedat;
    }
}

/// <summary>
/// Event raised when a coupon is redeemed.
/// </summary>
public class CouponRedeemedEvent : DomainEvent
{
    public Guid RedemptionId { get; }
    public Guid DiscountRuleId { get; }
    public string CouponCode { get; }
    public Guid UserId { get; }
    public Guid? OrderId { get; }
    public decimal OriginalAmount { get; }
    public decimal DiscountAmount { get; }
    public decimal FinalAmount { get; }
    public string? IpAddress { get; }
    public DateTime RedeemedAt { get; }

    public CouponRedeemedEvent(Guid redemptionid, Guid discountruleid, string couponcode, Guid userid, Guid? orderid, decimal originalamount, decimal discountamount, decimal finalamount, string? ipaddress, DateTime redeemedat)
    {
        RedemptionId = redemptionid;
        DiscountRuleId = discountruleid;
        CouponCode = couponcode;
        UserId = userid;
        OrderId = orderid;
        OriginalAmount = originalamount;
        DiscountAmount = discountamount;
        FinalAmount = finalamount;
        IpAddress = ipaddress;
        RedeemedAt = redeemedat;
    }
}

/// <summary>
/// Event raised when a coupon redemption is invalidated (e.g., due to refund).
/// </summary>
public class CouponRedemptionInvalidatedEvent : DomainEvent
{
    public Guid RedemptionId { get; }
    public Guid DiscountRuleId { get; }
    public string CouponCode { get; }
    public Guid UserId { get; }
    public decimal DiscountAmount { get; }
    public string Reason { get; }
    public DateTime InvalidatedAt { get; }

    public CouponRedemptionInvalidatedEvent(Guid redemptionid, Guid discountruleid, string couponcode, Guid userid, decimal discountamount, string reason, DateTime invalidatedat)
    {
        RedemptionId = redemptionid;
        DiscountRuleId = discountruleid;
        CouponCode = couponcode;
        UserId = userid;
        DiscountAmount = discountamount;
        Reason = reason;
        InvalidatedAt = invalidatedat;
    }
}

/// <summary>
/// Event raised when a discount is applied to an order.
/// </summary>
public class DiscountAppliedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid DiscountRuleId { get; }
    public string DiscountName { get; }
    public string DiscountType { get; }
    public decimal OriginalAmount { get; }
    public decimal DiscountAmount { get; }
    public decimal FinalAmount { get; }
    public string? CouponCode { get; }
    public DateTime AppliedAt { get; }

    public DiscountAppliedEvent(Guid orderid, Guid discountruleid, string discountname, string discounttype, decimal originalamount, decimal discountamount, decimal finalamount, string? couponcode, DateTime appliedat)
    {
        OrderId = orderid;
        DiscountRuleId = discountruleid;
        DiscountName = discountname;
        DiscountType = discounttype;
        OriginalAmount = originalamount;
        DiscountAmount = discountamount;
        FinalAmount = finalamount;
        CouponCode = couponcode;
        AppliedAt = appliedat;
    }
}

/// <summary>
/// Event raised when a discount application fails.
/// </summary>
public class DiscountApplicationFailedEvent : DomainEvent
{
    public Guid? OrderId { get; }
    public Guid DiscountRuleId { get; }
    public string DiscountName { get; }
    public string? CouponCode { get; }
    public string FailureReason { get; }
    public DateTime FailedAt { get; }

    public DiscountApplicationFailedEvent(Guid? orderid, Guid discountruleid, string discountname, string? couponcode, string failurereason, DateTime failedat)
    {
        OrderId = orderid;
        DiscountRuleId = discountruleid;
        DiscountName = discountname;
        CouponCode = couponcode;
        FailureReason = failurereason;
        FailedAt = failedat;
    }
}

/// <summary>
/// Event raised when a discount rule reaches its usage limit.
/// </summary>
public class DiscountRuleUsageLimitReachedEvent : DomainEvent
{
    public Guid DiscountRuleId { get; }
    public string Name { get; }
    public int UsageLimit { get; }
    public int CurrentUsage { get; }
    public DateTime LimitReachedAt { get; }

    public DiscountRuleUsageLimitReachedEvent(Guid discountruleid, string name, int usagelimit, int currentusage, DateTime limitreachedat)
    {
        DiscountRuleId = discountruleid;
        Name = name;
        UsageLimit = usagelimit;
        CurrentUsage = currentusage;
        LimitReachedAt = limitreachedat;
    }
}