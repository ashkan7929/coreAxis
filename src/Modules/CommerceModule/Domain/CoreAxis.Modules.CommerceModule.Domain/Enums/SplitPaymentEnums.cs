namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the type of party in a split payment allocation.
/// </summary>
public enum SplitPaymentPartyType
{
    /// <summary>
    /// Marketplace or platform owner.
    /// </summary>
    Marketplace = 0,
    
    /// <summary>
    /// Vendor or seller.
    /// </summary>
    Vendor = 1,
    
    /// <summary>
    /// Affiliate or referrer.
    /// </summary>
    Affiliate = 2,
    
    /// <summary>
    /// Payment processor or gateway.
    /// </summary>
    PaymentProcessor = 3,
    
    /// <summary>
    /// Tax authority.
    /// </summary>
    TaxAuthority = 4,
    
    /// <summary>
    /// Shipping provider.
    /// </summary>
    ShippingProvider = 5,
    
    /// <summary>
    /// Insurance provider.
    /// </summary>
    InsuranceProvider = 6,
    
    /// <summary>
    /// Other third party.
    /// </summary>
    Other = 99
}

/// <summary>
/// Represents the type of allocation calculation.
/// </summary>
public enum SplitPaymentAllocationType
{
    /// <summary>
    /// Percentage of the base amount.
    /// </summary>
    Percentage = 0,
    
    /// <summary>
    /// Fixed amount.
    /// </summary>
    FixedAmount = 1,
    
    /// <summary>
    /// Remaining amount after other allocations.
    /// </summary>
    Remainder = 2,
    
    /// <summary>
    /// Amount per item/unit.
    /// </summary>
    PerUnit = 3
}

/// <summary>
/// Represents the status of a split payment allocation.
/// </summary>
public enum SplitPaymentAllocationStatus
{
    /// <summary>
    /// Allocation is pending processing.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Allocation has been processed but not yet settled.
    /// </summary>
    Processed = 1,
    
    /// <summary>
    /// Allocation has been settled/paid out.
    /// </summary>
    Settled = 2,
    
    /// <summary>
    /// Allocation processing failed.
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Allocation was cancelled.
    /// </summary>
    Cancelled = 4,
    
    /// <summary>
    /// Allocation is on hold for review.
    /// </summary>
    OnHold = 5,
    
    /// <summary>
    /// Allocation was refunded.
    /// </summary>
    Refunded = 6
}

/// <summary>
/// Represents the priority level for split payment rules.
/// </summary>
public enum SplitPaymentRulePriority
{
    /// <summary>
    /// Lowest priority.
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 1,
    
    /// <summary>
    /// High priority.
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Critical priority - always processed first.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Represents the settlement frequency for split payments.
/// </summary>
public enum SplitPaymentSettlementFrequency
{
    /// <summary>
    /// Immediate settlement.
    /// </summary>
    Immediate = 0,
    
    /// <summary>
    /// Daily settlement.
    /// </summary>
    Daily = 1,
    
    /// <summary>
    /// Weekly settlement.
    /// </summary>
    Weekly = 2,
    
    /// <summary>
    /// Monthly settlement.
    /// </summary>
    Monthly = 3,
    
    /// <summary>
    /// Manual settlement.
    /// </summary>
    Manual = 4
}