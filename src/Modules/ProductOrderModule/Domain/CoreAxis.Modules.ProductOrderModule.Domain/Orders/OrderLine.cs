using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Orders;

public class OrderLine : EntityBase
{
    public Guid OrderId { get; private set; }
    public AssetCode AssetCode { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public string? Description { get; private set; }
    
    // Navigation property
    public Order Order { get; private set; } = null!;

    private OrderLine() { } // For EF Core

    private OrderLine(AssetCode assetCode, decimal quantity, decimal unitPrice, string? description = null)
    {
        Id = Guid.NewGuid();
        AssetCode = assetCode;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = quantity * unitPrice;
        Description = description;
    }

    public static OrderLine Create(AssetCode assetCode, decimal quantity, decimal unitPrice, string? description = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        
        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than zero", nameof(unitPrice));

        return new OrderLine(assetCode, quantity, unitPrice, description);
    }

    internal void SetOrderId(Guid orderId)
    {
        OrderId = orderId;
    }

    public void UpdatePrice(decimal newUnitPrice)
    {
        if (newUnitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than zero", nameof(newUnitPrice));
        
        UnitPrice = newUnitPrice;
        TotalPrice = Quantity * UnitPrice;
    }
}