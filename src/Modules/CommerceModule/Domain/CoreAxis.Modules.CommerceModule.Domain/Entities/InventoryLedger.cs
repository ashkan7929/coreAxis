using CoreAxis.SharedKernel;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a ledger entry for inventory movements and changes.
/// </summary>
public class InventoryLedger : EntityBase
{
    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid ProductId { get; private set; }
    
    /// <summary>
    /// Gets the SKU.
    /// </summary>
    public string Sku { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the location identifier.
    /// </summary>
    public Guid? LocationId { get; private set; }
    
    /// <summary>
    /// Gets the quantity change (positive for increases, negative for decreases).
    /// </summary>
    public decimal QuantityChange { get; private set; }
    
    /// <summary>
    /// Gets the quantity before this change.
    /// </summary>
    public decimal QuantityBefore { get; private set; }
    
    /// <summary>
    /// Gets the quantity after this change.
    /// </summary>
    public decimal QuantityAfter { get; private set; }
    
    /// <summary>
    /// Gets the reason for this inventory change.
    /// </summary>
    public InventoryLedgerReason Reason { get; private set; }
    
    /// <summary>
    /// Gets the reference identifier (e.g., order ID, adjustment ID).
    /// </summary>
    public Guid? ReferenceId { get; private set; }
    
    /// <summary>
    /// Gets the reference type (e.g., "Order", "Adjustment", "Receiving").
    /// </summary>
    public string? ReferenceType { get; private set; }
    
    /// <summary>
    /// Gets additional notes or comments.
    /// </summary>
    public string? Notes { get; private set; }
    
    /// <summary>
    /// Gets the correlation identifier for tracking related operations.
    /// </summary>
    public string? CorrelationId { get; private set; }
    
    /// <summary>
    /// Gets the transaction timestamp.
    /// </summary>
    public DateTime TransactionDate { get; private set; }
    
    /// <summary>
    /// Gets the user who performed this transaction.
    /// </summary>
    public Guid? PerformedBy { get; private set; }

    // Private constructor for EF Core
    private InventoryLedger() { }

    /// <summary>
    /// Creates a new inventory ledger entry.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="quantityChange">The quantity change</param>
    /// <param name="quantityBefore">The quantity before change</param>
    /// <param name="reason">The reason for the change</param>
    /// <param name="referenceId">The reference identifier</param>
    /// <param name="referenceType">The reference type</param>
    /// <param name="notes">Additional notes</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="performedBy">The user who performed the transaction</param>
    /// <returns>A new InventoryLedger instance</returns>
    public static InventoryLedger Create(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantityChange,
        decimal quantityBefore,
        InventoryLedgerReason reason,
        Guid? referenceId = null,
        string? referenceType = null,
        string? notes = null,
        string? correlationId = null,
        Guid? performedBy = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));
        
        if (quantityBefore < 0)
            throw new ArgumentException("Quantity before cannot be negative.", nameof(quantityBefore));

        var quantityAfter = quantityBefore + quantityChange;
        if (quantityAfter < 0)
            throw new ArgumentException("Resulting quantity cannot be negative.", nameof(quantityChange));

        var ledgerEntry = new InventoryLedger
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = sku,
            LocationId = locationId,
            QuantityChange = quantityChange,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            Reason = reason,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            Notes = notes,
            CorrelationId = correlationId,
            TransactionDate = DateTime.UtcNow,
            PerformedBy = performedBy,
            CreatedOn = DateTime.UtcNow,
            IsActive = true
        };

        ledgerEntry.AddDomainEvent(new InventoryLedgerEntryCreatedEvent(
            ledgerEntry.Id,
            productId,
            sku,
            locationId,
            quantityChange,
            quantityBefore,
            quantityAfter,
            reason,
            referenceId,
            referenceType,
            correlationId));

        return ledgerEntry;
    }

    /// <summary>
    /// Creates a ledger entry for inventory reservation.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="quantity">The quantity reserved</param>
    /// <param name="quantityBefore">The available quantity before reservation</param>
    /// <param name="orderId">The order identifier</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="performedBy">The user who performed the reservation</param>
    /// <returns>A new InventoryLedger instance for reservation</returns>
    public static InventoryLedger CreateReservationEntry(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantity,
        decimal quantityBefore,
        Guid orderId,
        string? correlationId = null,
        Guid? performedBy = null)
    {
        return Create(
            productId,
            sku,
            locationId,
            -quantity, // Negative because it reduces available quantity
            quantityBefore,
            InventoryLedgerReason.Reserve,
            orderId,
            "Order",
            $"Reserved {quantity} units for order {orderId}",
            correlationId,
            performedBy);
    }

    /// <summary>
    /// Creates a ledger entry for inventory commitment.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="quantity">The quantity committed</param>
    /// <param name="quantityBefore">The on-hand quantity before commitment</param>
    /// <param name="orderId">The order identifier</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="performedBy">The user who performed the commitment</param>
    /// <returns>A new InventoryLedger instance for commitment</returns>
    public static InventoryLedger CreateCommitmentEntry(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantity,
        decimal quantityBefore,
        Guid orderId,
        string? correlationId = null,
        Guid? performedBy = null)
    {
        return Create(
            productId,
            sku,
            locationId,
            -quantity, // Negative because it reduces on-hand quantity
            quantityBefore,
            InventoryLedgerReason.Commit,
            orderId,
            "Order",
            $"Committed {quantity} units for order {orderId}",
            correlationId,
            performedBy);
    }

    /// <summary>
    /// Creates a ledger entry for inventory release.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="quantity">The quantity released</param>
    /// <param name="quantityBefore">The available quantity before release</param>
    /// <param name="orderId">The order identifier</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="performedBy">The user who performed the release</param>
    /// <returns>A new InventoryLedger instance for release</returns>
    public static InventoryLedger CreateReleaseEntry(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantity,
        decimal quantityBefore,
        Guid orderId,
        string? correlationId = null,
        Guid? performedBy = null)
    {
        return Create(
            productId,
            sku,
            locationId,
            quantity, // Positive because it increases available quantity
            quantityBefore,
            InventoryLedgerReason.Release,
            orderId,
            "Order",
            $"Released {quantity} units from order {orderId}",
            correlationId,
            performedBy);
    }

    /// <summary>
    /// Creates a ledger entry for inventory adjustment.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="quantityChange">The quantity change</param>
    /// <param name="quantityBefore">The quantity before adjustment</param>
    /// <param name="adjustmentId">The adjustment identifier</param>
    /// <param name="reason">The reason for adjustment</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="performedBy">The user who performed the adjustment</param>
    /// <returns>A new InventoryLedger instance for adjustment</returns>
    public static InventoryLedger CreateAdjustmentEntry(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantityChange,
        decimal quantityBefore,
        Guid adjustmentId,
        string reason,
        string? correlationId = null,
        Guid? performedBy = null)
    {
        return Create(
            productId,
            sku,
            locationId,
            quantityChange,
            quantityBefore,
            InventoryLedgerReason.Adjust,
            adjustmentId,
            "Adjustment",
            reason,
            correlationId,
            performedBy);
    }

    /// <summary>
    /// Creates a ledger entry for inventory receiving.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="quantity">The quantity received</param>
    /// <param name="quantityBefore">The quantity before receiving</param>
    /// <param name="receiptId">The receipt identifier</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="performedBy">The user who performed the receiving</param>
    /// <returns>A new InventoryLedger instance for receiving</returns>
    public static InventoryLedger CreateReceivingEntry(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal quantity,
        decimal quantityBefore,
        Guid receiptId,
        string? correlationId = null,
        Guid? performedBy = null)
    {
        return Create(
            productId,
            sku,
            locationId,
            quantity, // Positive because it increases inventory
            quantityBefore,
            InventoryLedgerReason.Received,
            receiptId,
            "Receipt",
            $"Received {quantity} units",
            correlationId,
            performedBy);
    }

    /// <summary>
    /// Gets a value indicating whether this is an increase in inventory.
    /// </summary>
    public bool IsIncrease => QuantityChange > 0;

    /// <summary>
    /// Gets a value indicating whether this is a decrease in inventory.
    /// </summary>
    public bool IsDecrease => QuantityChange < 0;

    /// <summary>
    /// Gets the absolute value of the quantity change.
    /// </summary>
    public decimal AbsoluteQuantityChange => Math.Abs(QuantityChange);
}