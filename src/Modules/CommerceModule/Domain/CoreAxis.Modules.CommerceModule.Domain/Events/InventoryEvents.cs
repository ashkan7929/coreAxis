using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

#region Inventory Item Events

/// <summary>
/// Event raised when inventory is adjusted.
/// </summary>
public class InventoryAdjustedEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal OldQuantity { get; }
    public decimal NewQuantity { get; }
    public decimal AdjustmentAmount { get; }
    public string Reason { get; }
    public string? CorrelationId { get; }

    public InventoryAdjustedEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal oldQuantity,
        decimal newQuantity,
        decimal adjustmentAmount,
        string reason,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
        AdjustmentAmount = adjustmentAmount;
        Reason = reason;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory is reserved.
/// </summary>
public class InventoryReservedEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal ReservedQuantity { get; }
    public decimal AvailableQuantity { get; }
    public Guid OrderId { get; }
    public string? CorrelationId { get; }

    public InventoryReservedEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal reservedQuantity,
        decimal availableQuantity,
        Guid orderId,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        ReservedQuantity = reservedQuantity;
        AvailableQuantity = availableQuantity;
        OrderId = orderId;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when reserved inventory is committed.
/// </summary>
public class InventoryCommittedEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal CommittedQuantity { get; }
    public decimal OnHandQuantity { get; }
    public Guid OrderId { get; }
    public string? CorrelationId { get; }

    public InventoryCommittedEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal committedQuantity,
        decimal onHandQuantity,
        Guid orderId,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        CommittedQuantity = committedQuantity;
        OnHandQuantity = onHandQuantity;
        OrderId = orderId;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when reserved inventory is released.
/// </summary>
public class InventoryReleasedEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal ReleasedQuantity { get; }
    public decimal AvailableQuantity { get; }
    public Guid OrderId { get; }
    public string Reason { get; }
    public string? CorrelationId { get; }

    public InventoryReleasedEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal releasedQuantity,
        decimal availableQuantity,
        Guid orderId,
        string reason,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        ReleasedQuantity = releasedQuantity;
        AvailableQuantity = availableQuantity;
        OrderId = orderId;
        Reason = reason;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory tracking is enabled or disabled.
/// </summary>
public class InventoryTrackingChangedEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public bool IsTracked { get; }
    public string? CorrelationId { get; }

    public InventoryTrackingChangedEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        bool isTracked,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        IsTracked = isTracked;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when reorder threshold is updated.
/// </summary>
public class InventoryReorderThresholdUpdatedEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal OldThreshold { get; }
    public decimal NewThreshold { get; }
    public string? CorrelationId { get; }

    public InventoryReorderThresholdUpdatedEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal oldThreshold,
        decimal newThreshold,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        OldThreshold = oldThreshold;
        NewThreshold = newThreshold;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory falls below reorder threshold.
/// </summary>
public class InventoryBelowReorderThresholdEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal CurrentQuantity { get; }
    public decimal ReorderThreshold { get; }
    public string? CorrelationId { get; }

    public InventoryBelowReorderThresholdEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal currentQuantity,
        decimal reorderThreshold,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        CurrentQuantity = currentQuantity;
        ReorderThreshold = reorderThreshold;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory is out of stock.
/// </summary>
public class InventoryOutOfStockEvent : DomainEvent
{
    public Guid InventoryItemId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public string? CorrelationId { get; }

    public InventoryOutOfStockEvent(
        Guid inventoryItemId,
        Guid productId,
        string sku,
        Guid? locationId,
        string? correlationId = null)
    {
        InventoryItemId = inventoryItemId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        CorrelationId = correlationId;
    }
}

#endregion

#region Inventory Reservation Events

/// <summary>
/// Event raised when an inventory reservation is created.
/// </summary>
public class InventoryReservationCreatedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public IEnumerable<ReservationItem> Items { get; }
    public DateTime ExpiresAt { get; }
    public string CorrelationId { get; }

    public InventoryReservationCreatedEvent(
        Guid reservationId,
        Guid orderId,
        Guid userId,
        IEnumerable<ReservationItem> items,
        DateTime expiresAt,
        string correlationId)
    {
        ReservationId = reservationId;
        OrderId = orderId;
        UserId = userId;
        Items = items;
        ExpiresAt = expiresAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when an inventory reservation is committed.
/// </summary>
public class InventoryReservationCommittedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public IEnumerable<ReservationItem> Items { get; }
    public string CorrelationId { get; }

    public InventoryReservationCommittedEvent(
        Guid reservationId,
        Guid orderId,
        Guid userId,
        IEnumerable<ReservationItem> items,
        string correlationId)
    {
        ReservationId = reservationId;
        OrderId = orderId;
        UserId = userId;
        Items = items;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when an inventory reservation is released.
/// </summary>
public class InventoryReservationReleasedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public IEnumerable<ReservationItem> Items { get; }
    public string Reason { get; }
    public string CorrelationId { get; }

    public InventoryReservationReleasedEvent(
        Guid reservationId,
        Guid orderId,
        Guid userId,
        IEnumerable<ReservationItem> items,
        string reason,
        string correlationId)
    {
        ReservationId = reservationId;
        OrderId = orderId;
        UserId = userId;
        Items = items;
        Reason = reason;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when an inventory reservation expires.
/// </summary>
public class InventoryReservationExpiredEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public IEnumerable<ReservationItem> Items { get; }
    public string CorrelationId { get; }

    public InventoryReservationExpiredEvent(
        Guid reservationId,
        Guid orderId,
        Guid userId,
        IEnumerable<ReservationItem> items,
        string correlationId)
    {
        ReservationId = reservationId;
        OrderId = orderId;
        UserId = userId;
        Items = items;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when an inventory reservation is extended.
/// </summary>
public class InventoryReservationExtendedEvent : DomainEvent
{
    public Guid ReservationId { get; }
    public Guid OrderId { get; }
    public DateTime NewExpiresAt { get; }
    public string CorrelationId { get; }

    public InventoryReservationExtendedEvent(
        Guid reservationId,
        Guid orderId,
        DateTime newExpiresAt,
        string correlationId)
    {
        ReservationId = reservationId;
        OrderId = orderId;
        NewExpiresAt = newExpiresAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a reservation fails due to insufficient inventory.
/// </summary>
public class InventoryReservationFailedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public IEnumerable<ReservationItem> RequestedItems { get; }
    public IEnumerable<ReservationItem> UnavailableItems { get; }
    public string Reason { get; }
    public string CorrelationId { get; }

    public InventoryReservationFailedEvent(
        Guid orderId,
        Guid userId,
        IEnumerable<ReservationItem> requestedItems,
        IEnumerable<ReservationItem> unavailableItems,
        string reason,
        string correlationId)
    {
        OrderId = orderId;
        UserId = userId;
        RequestedItems = requestedItems;
        UnavailableItems = unavailableItems;
        Reason = reason;
        CorrelationId = correlationId;
    }
}

#endregion

#region Inventory Ledger Events

/// <summary>
/// Event raised when an inventory ledger entry is created.
/// </summary>
public class InventoryLedgerEntryCreatedEvent : DomainEvent
{
    public Guid LedgerEntryId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal QuantityChange { get; }
    public decimal QuantityBefore { get; }
    public decimal QuantityAfter { get; }
    public InventoryLedgerReason Reason { get; }
    public Guid? ReferenceId { get; }
    public string? ReferenceType { get; }
    public string? CorrelationId { get; }

    public InventoryLedgerEntryCreatedEvent(
        Guid ledgerEntryId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantityChange,
        decimal quantityBefore,
        decimal quantityAfter,
        InventoryLedgerReason reason,
        Guid? referenceId,
        string? referenceType,
        string? correlationId)
    {
        LedgerEntryId = ledgerEntryId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        QuantityChange = quantityChange;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        Reason = reason;
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory movement is detected.
/// </summary>
public class InventoryMovementEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? FromLocationId { get; }
    public Guid? ToLocationId { get; }
    public decimal Quantity { get; }
    public InventoryLedgerReason Reason { get; }
    public Guid? ReferenceId { get; }
    public string? CorrelationId { get; }

    public InventoryMovementEvent(
        Guid productId,
        string sku,
        Guid? fromLocationId,
        Guid? toLocationId,
        decimal quantity,
        InventoryLedgerReason reason,
        Guid? referenceId,
        string? correlationId)
    {
        ProductId = productId;
        Sku = sku;
        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;
        Quantity = quantity;
        Reason = reason;
        ReferenceId = referenceId;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory discrepancy is detected.
/// </summary>
public class InventoryDiscrepancyDetectedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal ExpectedQuantity { get; }
    public decimal ActualQuantity { get; }
    public decimal Variance { get; }
    public string DetectionMethod { get; }
    public string? CorrelationId { get; }

    public InventoryDiscrepancyDetectedEvent(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal expectedQuantity,
        decimal actualQuantity,
        decimal variance,
        string detectionMethod,
        string? correlationId)
    {
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        ExpectedQuantity = expectedQuantity;
        ActualQuantity = actualQuantity;
        Variance = variance;
        DetectionMethod = detectionMethod;
        CorrelationId = correlationId;
    }
}

#endregion

#region Integration Events

/// <summary>
/// Event raised when inventory needs to be synchronized with external systems.
/// </summary>
public class InventorySyncRequiredEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal CurrentQuantity { get; }
    public string SyncReason { get; }
    public string? CorrelationId { get; }

    public InventorySyncRequiredEvent(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal currentQuantity,
        string syncReason,
        string? correlationId)
    {
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        CurrentQuantity = currentQuantity;
        SyncReason = syncReason;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory allocation fails.
/// </summary>
public class InventoryAllocationFailedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal RequestedQuantity { get; }
    public decimal AvailableQuantity { get; }
    public string FailureReason { get; }
    public string? CorrelationId { get; }

    public InventoryAllocationFailedEvent(
        Guid orderId,
        Guid productId,
        string sku,
        Guid? locationId,
        decimal requestedQuantity,
        decimal availableQuantity,
        string failureReason,
        string? correlationId)
    {
        OrderId = orderId;
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
        FailureReason = failureReason;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when inventory needs replenishment.
/// </summary>
public class InventoryReplenishmentRequiredEvent : DomainEvent
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public Guid? LocationId { get; }
    public decimal CurrentQuantity { get; }
    public decimal ReorderThreshold { get; }
    public decimal SuggestedOrderQuantity { get; }
    public string? CorrelationId { get; }

    public InventoryReplenishmentRequiredEvent(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal currentQuantity,
        decimal reorderThreshold,
        decimal suggestedOrderQuantity,
        string? correlationId)
    {
        ProductId = productId;
        Sku = sku;
        LocationId = locationId;
        CurrentQuantity = currentQuantity;
        ReorderThreshold = reorderThreshold;
        SuggestedOrderQuantity = suggestedOrderQuantity;
        CorrelationId = correlationId;
    }
}

#endregion