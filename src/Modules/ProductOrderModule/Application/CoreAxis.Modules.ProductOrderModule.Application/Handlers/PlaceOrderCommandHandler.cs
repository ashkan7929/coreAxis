using CoreAxis.Modules.ProductOrderModule.Application.Commands;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Handlers;

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(IOrderRepository orderRepository, ILogger<PlaceOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Check for existing order with same idempotency key
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existingOrder != null)
            {
                _logger.LogInformation("Order with IdempotencyKey {IdempotencyKey} already exists. Returning existing order {OrderId}", 
                    request.IdempotencyKey, existingOrder.Id);
                
                return MapToDto(existingOrder);
            }
        }

        // Create order lines
        var orderLines = request.OrderLines.Select(ol => 
            OrderLine.Create(
                AssetCode.Create(ol.AssetCode),
                ol.Quantity,
                ol.UnitPrice
            )).ToList();

        // Create order with order lines
        var order = Order.Create(
            request.UserId,
            AssetCode.Create(request.AssetCode),
            request.TotalAmount,
            orderLines
        );

        // Set idempotency key if provided
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            order.SetIdempotencyKey(request.IdempotencyKey);
        }

        // Save order
        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        _logger.LogInformation("New order created with ID {OrderId} for user {UserId}. IdempotencyKey: {IdempotencyKey}", 
            order.Id, request.UserId, request.IdempotencyKey ?? "None");

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            AssetCode = order.AssetCode.Value,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderLines = order.OrderLines.Select(ol => new OrderLineDto
            {
                Id = ol.Id,
                OrderId = ol.OrderId,
                AssetCode = ol.AssetCode.Value,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                TotalPrice = ol.TotalPrice,
                Description = ol.Description
            }).ToList()
        };
    }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        
        if (order == null || order.UserId != request.UserId)
            return false;

        order.Cancel();
        await _orderRepository.SaveChangesAsync();
        
        return true;
    }
}