using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands;

public class PlaceOrderCommand : IRequest<OrderDto>
{
    public string UserId { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<PlaceOrderLineDto> OrderLines { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}

public class CancelOrderCommand : IRequest<bool>
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
}