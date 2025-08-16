using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderLineDto> OrderLines { get; set; } = new();
}

public class OrderLineDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Description { get; set; }
}

public class PlaceOrderDto
{
    public string AssetCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<PlaceOrderLineDto> OrderLines { get; set; } = new();
}

public class PlaceOrderLineDto
{
    public string AssetCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}