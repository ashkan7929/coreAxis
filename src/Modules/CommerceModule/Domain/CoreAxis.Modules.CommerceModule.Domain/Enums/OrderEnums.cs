namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created but not yet confirmed.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Order has been confirmed and is being processed.
    /// </summary>
    Confirmed = 1,
    
    /// <summary>
    /// Order is being prepared for shipment.
    /// </summary>
    Processing = 2,
    
    /// <summary>
    /// Order has been shipped.
    /// </summary>
    Shipped = 3,
    
    /// <summary>
    /// Order has been delivered to the customer.
    /// </summary>
    Delivered = 4,
    
    /// <summary>
    /// Order has been completed successfully.
    /// </summary>
    Completed = 5,
    
    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled = 6,
    
    /// <summary>
    /// Order has been refunded.
    /// </summary>
    Refunded = 7,
    
    /// <summary>
    /// Order has been returned by the customer.
    /// </summary>
    Returned = 8,
    
    /// <summary>
    /// Order is on hold pending review.
    /// </summary>
    OnHold = 9,
    
    /// <summary>
    /// Order has failed due to payment or other issues.
    /// </summary>
    Failed = 10
}

/// <summary>
/// Represents the priority level of an order.
/// </summary>
public enum OrderPriority
{
    /// <summary>
    /// Low priority order.
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority order.
    /// </summary>
    Normal = 1,
    
    /// <summary>
    /// High priority order.
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Urgent priority order.
    /// </summary>
    Urgent = 3
}