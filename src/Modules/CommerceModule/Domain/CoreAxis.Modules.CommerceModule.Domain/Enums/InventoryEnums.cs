namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the status of an inventory reservation.
/// </summary>
public enum InventoryReservationStatus
{
    /// <summary>
    /// The reservation is pending and active.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// The reservation has been committed (converted to actual sale).
    /// </summary>
    Committed = 1,
    
    /// <summary>
    /// The reservation has been released (cancelled).
    /// </summary>
    Released = 2,
    
    /// <summary>
    /// The reservation has expired.
    /// </summary>
    Expired = 3
}

/// <summary>
/// Represents the reason for an inventory ledger entry.
/// </summary>
public enum InventoryLedgerReason
{
    /// <summary>
    /// Stock reserved for an order.
    /// </summary>
    Reserve = 0,
    
    /// <summary>
    /// Stock reservation committed.
    /// </summary>
    Commit = 1,
    
    /// <summary>
    /// Stock reservation released.
    /// </summary>
    Release = 2,
    
    /// <summary>
    /// Manual adjustment by administrator.
    /// </summary>
    Adjust = 3,
    
    /// <summary>
    /// Initial stock entry.
    /// </summary>
    Initial = 4,
    
    /// <summary>
    /// Stock received from supplier.
    /// </summary>
    Received = 5,
    
    /// <summary>
    /// Stock damaged or lost.
    /// </summary>
    Damaged = 6,
    
    /// <summary>
    /// Stock sold to customer.
    /// </summary>
    Sold = 7,
    
    /// <summary>
    /// Stock returned by customer.
    /// </summary>
    Returned = 8,
    
    /// <summary>
    /// Stock transferred to another location.
    /// </summary>
    Transfer = 9,
    
    /// <summary>
    /// Stock counted during physical inventory.
    /// </summary>
    PhysicalCount = 10
}

/// <summary>
/// Represents the type of reference for inventory ledger entries.
/// </summary>
public enum InventoryLedgerReferenceType
{
    /// <summary>
    /// No specific reference type.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Reference to an order.
    /// </summary>
    Order = 1,
    
    /// <summary>
    /// Reference to a purchase order.
    /// </summary>
    PurchaseOrder = 2,
    
    /// <summary>
    /// Reference to a transfer.
    /// </summary>
    Transfer = 3,
    
    /// <summary>
    /// Reference to an adjustment.
    /// </summary>
    Adjustment = 4,
    
    /// <summary>
    /// Reference to a physical count.
    /// </summary>
    PhysicalCount = 5,
    
    /// <summary>
    /// Reference to a reservation.
    /// </summary>
    Reservation = 6
}