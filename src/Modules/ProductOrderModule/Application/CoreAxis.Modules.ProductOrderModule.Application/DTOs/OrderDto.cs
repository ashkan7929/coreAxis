using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;

namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? LockedPrice { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    public List<OrderLineDto> OrderLines { get; set; } = new();
}

public class OrderLineDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public class PlaceOrderDto
{
    public string AssetCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<PlaceOrderLineDto> OrderLines { get; set; } = new();
    public object? ApplicationData { get; set; }
}

public class PlaceOrderLineDto
{
    public string AssetCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}