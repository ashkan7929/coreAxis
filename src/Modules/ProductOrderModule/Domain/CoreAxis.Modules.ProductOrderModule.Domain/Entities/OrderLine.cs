using CoreAxis.SharedKernel;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Entities;

/// <summary>
/// Represents a line item within an order.
/// </summary>
public class OrderLine : EntityBase
{
    public Guid OrderId { get; private set; }
    public AssetCode AssetCode { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public Money? UnitPrice { get; private set; }
    public Money? LineTotal { get; private set; }
    public string? Notes { get; private set; }

    // Navigation property
    public Order Order { get; private set; } = null!;

    // Private constructor for EF Core
    private OrderLine() { }

    /// <summary>
    /// Creates a new order line.
    /// </summary>
    /// <param name="orderId">The ID of the parent order</param>
    /// <param name="assetCode">The asset code for this line</param>
    /// <param name="quantity">The quantity for this line</param>
    /// <param name="unitPrice">Optional unit price</param>
    /// <param name="notes">Optional notes for this line</param>
    /// <returns>A new OrderLine instance</returns>
    public static OrderLine Create(
        Guid orderId,
        AssetCode assetCode,
        decimal quantity,
        Money? unitPrice = null,
        string? notes = null)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        var orderLine = new OrderLine
        {
            OrderId = orderId,
            AssetCode = assetCode,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Notes = notes
        };

        if (unitPrice != null)
        {
            orderLine.LineTotal = unitPrice * quantity;
        }

        return orderLine;
    }

    /// <summary>
    /// Updates the unit price and recalculates the line total.
    /// </summary>
    /// <param name="unitPrice">The new unit price</param>
    public void UpdateUnitPrice(Money unitPrice)
    {
        if (unitPrice == null)
            throw new ArgumentNullException(nameof(unitPrice));

        if (!unitPrice.IsPositive)
            throw new ArgumentException("Unit price must be positive.", nameof(unitPrice));

        UnitPrice = unitPrice;
        LineTotal = unitPrice * Quantity;
        LastModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the quantity and recalculates the line total.
    /// </summary>
    /// <param name="quantity">The new quantity</param>
    public void UpdateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        Quantity = quantity;
        
        if (UnitPrice != null)
        {
            LineTotal = UnitPrice * quantity;
        }
        
        LastModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the notes for this order line.
    /// </summary>
    /// <param name="notes">The new notes</param>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        LastModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this order line has a valid price.
    /// </summary>
    /// <returns>True if the line has a positive unit price, false otherwise</returns>
    public bool HasValidPrice()
    {
        return UnitPrice != null && UnitPrice.IsPositive;
    }

    /// <summary>
    /// Gets the total value of this line.
    /// </summary>
    /// <returns>The line total, or zero if no unit price is set</returns>
    public Money GetLineTotal()
    {
        if (UnitPrice == null)
            return Money.Zero("USD"); // Default currency, should be configurable

        return LineTotal ?? Money.Zero(UnitPrice?.Currency ?? "USD");
    }
}