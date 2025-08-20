namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the scope of a discount rule.
/// </summary>
public enum DiscountScope
{
    /// <summary>
    /// Discount applies to specific products.
    /// </summary>
    Product = 0,
    
    /// <summary>
    /// Discount applies to product categories.
    /// </summary>
    Category = 1,
    
    /// <summary>
    /// Discount applies to the entire cart.
    /// </summary>
    Cart = 2
}

/// <summary>
/// Represents the type of discount calculation.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Percentage-based discount.
    /// </summary>
    Percent = 0,
    
    /// <summary>
    /// Fixed amount discount.
    /// </summary>
    Fixed = 1
}

/// <summary>
/// Represents the status of a subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and current.
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Subscription payment is past due.
    /// </summary>
    PastDue = 1,
    
    /// <summary>
    /// Subscription has been canceled.
    /// </summary>
    Canceled = 2,
    
    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 3
}

/// <summary>
/// Represents the status of a subscription invoice.
/// </summary>
public enum SubscriptionInvoiceStatus
{
    /// <summary>
    /// Invoice is pending payment.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Invoice has been paid.
    /// </summary>
    Paid = 1,
    
    /// <summary>
    /// Invoice payment failed.
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Invoice has been refunded.
    /// </summary>
    Refunded = 3
}

/// <summary>
/// Represents the component of a split payment.
/// </summary>
public enum PaymentComponent
{
    /// <summary>
    /// Payment to the provider/supplier.
    /// </summary>
    Provider = 0,
    
    /// <summary>
    /// Payment to the seller.
    /// </summary>
    Seller = 1,
    
    /// <summary>
    /// Commission payment.
    /// </summary>
    Commission = 2,
    
    /// <summary>
    /// Fee payment.
    /// </summary>
    Fee = 3
}

/// <summary>
/// Represents the status of a refund request.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund request is pending approval.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Refund request has been approved.
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Refund request has been rejected.
    /// </summary>
    Rejected = 2,
    
    /// <summary>
    /// Refund has been processed.
    /// </summary>
    Processed = 3,
    
    /// <summary>
    /// Refund processing failed.
    /// </summary>
    Failed = 4
}

/// <summary>
/// Represents the status of a reconciliation record.
/// </summary>
public enum ReconciliationStatus
{
    /// <summary>
    /// Record has been imported but not yet matched.
    /// </summary>
    Imported = 0,
    
    /// <summary>
    /// Record has been matched with an order.
    /// </summary>
    Matched = 1,
    
    /// <summary>
    /// Record could not be matched or has discrepancies.
    /// </summary>
    Mismatched = 2,
    
    /// <summary>
    /// Mismatch has been resolved.
    /// </summary>
    Resolved = 3
}