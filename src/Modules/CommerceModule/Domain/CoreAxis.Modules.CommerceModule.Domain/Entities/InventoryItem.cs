using CoreAxis.SharedKernel;
using CoreAxis.Modules.CommerceModule.Domain.Events;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents an inventory item with stock tracking capabilities.
/// </summary>
public class InventoryItem : EntityBase
{
    /// <summary>
    /// Gets the product identifier this inventory item represents.
    /// </summary>
    public Guid ProductId { get; private set; }
    
    /// <summary>
    /// Gets the SKU (Stock Keeping Unit) for this inventory item.
    /// </summary>
    public string Sku { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the location identifier where this inventory is stored.
    /// </summary>
    public Guid? LocationId { get; private set; }
    
    /// <summary>
    /// Gets the quantity currently on hand.
    /// </summary>
    public decimal OnHand { get; private set; }
    
    /// <summary>
    /// Gets the quantity currently reserved for orders.
    /// </summary>
    public decimal Reserved { get; private set; }
    
    /// <summary>
    /// Gets the threshold below which reordering should be triggered.
    /// </summary>
    public decimal ReorderThreshold { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether this inventory item is tracked.
    /// </summary>
    public bool IsTracked { get; private set; }
    
    /// <summary>
    /// Gets the available quantity (OnHand - Reserved).
    /// </summary>
    public decimal Available => OnHand - Reserved;
    
    /// <summary>
    /// Gets a value indicating whether this item needs reordering.
    /// </summary>
    public bool NeedsReorder => Available <= ReorderThreshold;

    // Private constructor for EF Core
    private InventoryItem() { }

    /// <summary>
    /// Creates a new inventory item.
    /// </summary>
    /// <param name="productId">The product identifier</param>
    /// <param name="sku">The SKU</param>
    /// <param name="locationId">The location identifier</param>
    /// <param name="initialQuantity">The initial quantity on hand</param>
    /// <param name="reorderThreshold">The reorder threshold</param>
    /// <param name="isTracked">Whether this item is tracked</param>
    /// <returns>A new InventoryItem instance</returns>
    public static InventoryItem Create(
        Guid productId,
        string sku,
        Guid? locationId,
        decimal initialQuantity = 0,
        decimal reorderThreshold = 0,
        bool isTracked = true)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));
        
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative.", nameof(initialQuantity));
        
        if (reorderThreshold < 0)
            throw new ArgumentException("Reorder threshold cannot be negative.", nameof(reorderThreshold));

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = sku,
            LocationId = locationId,
            OnHand = initialQuantity,
            Reserved = 0,
            ReorderThreshold = reorderThreshold,
            IsTracked = isTracked,
            CreatedOn = DateTime.UtcNow,
            IsActive = true
        };

        if (initialQuantity > 0)
        {
            item.AddDomainEvent(new InventoryAdjustedEvent(
                item.Id,
                item.ProductId,
                item.Sku,
                item.LocationId,
                0,
                initialQuantity,
                initialQuantity,
                "Initial stock"));
        }

        return item;
    }

    /// <summary>
    /// Reserves the specified quantity for an order.
    /// </summary>
    /// <param name="quantity">The quantity to reserve</param>
    /// <param name="orderId">The order identifier</param>
    /// <returns>True if reservation was successful, false if insufficient stock</returns>
    public bool Reserve(decimal quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        
        if (!IsTracked)
            return true; // Always allow reservation for non-tracked items
        
        if (Available < quantity)
            return false; // Insufficient stock
        
        Reserved += quantity;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new InventoryReservedEvent(
            Id,
            ProductId,
            Sku,
            LocationId,
            quantity,
            Available,
            orderId));
        
        return true;
    }

    /// <summary>
    /// Commits a reservation, reducing the on-hand quantity.
    /// </summary>
    /// <param name="quantity">The quantity to commit</param>
    /// <param name="orderId">The order identifier</param>
    public void Commit(decimal quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        
        if (Reserved < quantity)
            throw new InvalidOperationException("Cannot commit more than reserved quantity.");
        
        Reserved -= quantity;
        OnHand -= quantity;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new InventoryCommittedEvent(
            Id,
            ProductId,
            Sku,
            LocationId,
            quantity,
            OnHand,
            orderId));
    }

    /// <summary>
    /// Releases a reservation, making the quantity available again.
    /// </summary>
    /// <param name="quantity">The quantity to release</param>
    /// <param name="orderId">The order identifier</param>
    public void Release(decimal quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        
        if (Reserved < quantity)
            throw new InvalidOperationException("Cannot release more than reserved quantity.");
        
        Reserved -= quantity;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new InventoryReleasedEvent(
            Id,
            ProductId,
            Sku,
            LocationId,
            quantity,
            Available,
            orderId,
            "Reservation released"));
    }

    /// <summary>
    /// Adjusts the on-hand quantity.
    /// </summary>
    /// <param name="delta">The change in quantity (positive or negative)</param>
    /// <param name="reason">The reason for the adjustment</param>
    public void Adjust(decimal delta, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));
        
        var oldOnHand = OnHand;
        var newOnHand = OnHand + delta;
        if (newOnHand < 0)
            throw new InvalidOperationException("Adjustment would result in negative on-hand quantity.");
        
        OnHand = newOnHand;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new InventoryAdjustedEvent(
            Id,
            ProductId,
            Sku,
            LocationId,
            oldOnHand,
            newOnHand,
            delta,
            reason));
    }

    /// <summary>
    /// Updates the reorder threshold.
    /// </summary>
    /// <param name="threshold">The new reorder threshold</param>
    public void UpdateReorderThreshold(decimal threshold)
    {
        if (threshold < 0)
            throw new ArgumentException("Reorder threshold cannot be negative.", nameof(threshold));
        
        ReorderThreshold = threshold;
        LastModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the tracking status for this inventory item.
    /// </summary>
    /// <param name="isTracked">Whether this item should be tracked</param>
    public void SetTracking(bool isTracked)
    {
        IsTracked = isTracked;
        LastModifiedOn = DateTime.UtcNow;
    }
}