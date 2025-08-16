namespace CoreAxis.Modules.ProductOrderModule.Domain.Enums;

/// <summary>
/// Represents the status of an order in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been placed but not yet processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Price has been locked for the order.
    /// </summary>
    PriceLocked = 1,

    /// <summary>
    /// Order has been confirmed and is being processed.
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Order has been completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Order has expired (e.g., price lock expired).
    /// </summary>
    Expired = 5
}