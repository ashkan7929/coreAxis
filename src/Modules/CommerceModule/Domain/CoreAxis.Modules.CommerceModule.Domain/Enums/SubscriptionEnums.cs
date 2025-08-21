namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the billing cycle for subscriptions.
/// </summary>
public enum BillingCycle
{
    /// <summary>
    /// Daily billing cycle.
    /// </summary>
    Daily = 0,
    
    /// <summary>
    /// Weekly billing cycle.
    /// </summary>
    Weekly = 1,
    
    /// <summary>
    /// Monthly billing cycle.
    /// </summary>
    Monthly = 2,
    
    /// <summary>
    /// Quarterly billing cycle (every 3 months).
    /// </summary>
    Quarterly = 3,
    
    /// <summary>
    /// Semi-annual billing cycle (every 6 months).
    /// </summary>
    SemiAnnual = 4,
    
    /// <summary>
    /// Annual billing cycle (yearly).
    /// </summary>
    Annual = 5,
    
    /// <summary>
    /// Biennial billing cycle (every 2 years).
    /// </summary>
    Biennial = 6,
    
    /// <summary>
    /// One-time payment (no recurring billing).
    /// </summary>
    OneTime = 7
}