using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Orders;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    string Status,
    string? Notes = null
) : IRequest<OrderDto>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {request.OrderId} not found.");
            }

            var previousStatus = order.Status;
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Notes))
            {
                order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\n{request.Notes}";
            }

            // Handle status-specific logic
            await HandleStatusChange(order, previousStatus, request.Status);

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Order status updated successfully. OrderId: {OrderId}, Status: {Status}", 
                order.Id, request.Status);

            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                FinalAmount = order.FinalAmount,
                Notes = order.Notes,
                OrderDate = order.OrderDate,
                ShippingDate = order.ShippingDate,
                DeliveryDate = order.DeliveryDate,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    InventoryItemId = oi.InventoryItemId,
                    AssetCode = oi.AssetCode,
                    ItemName = oi.ItemName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice,
                    Currency = oi.Currency,
                    DiscountAmount = oi.DiscountAmount,
                    Notes = oi.Notes,
                    CreatedAt = oi.CreatedAt,
                    UpdatedAt = oi.UpdatedAt
                }).ToList() ?? new List<OrderItemDto>(),
                Payment = null,
                AppliedDiscounts = new List<DiscountRuleDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status. OrderId: {OrderId}", request.OrderId);
            throw;
        }
    }

    private async Task HandleStatusChange(Order order, string previousStatus, string newStatus)
    {
        switch (newStatus.ToLower())
        {
            case "shipped":
                order.ShippingDate = DateTime.UtcNow;
                break;
            case "delivered":
                order.DeliveryDate = DateTime.UtcNow;
                break;
            case "cancelled":
                // Release reserved inventory
                if (order.OrderItems != null)
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var inventoryItem = await _inventoryRepository.GetByIdAsync(orderItem.InventoryItemId);
                        if (inventoryItem != null)
                        {
                            inventoryItem.AvailableQuantity += orderItem.Quantity;
                            inventoryItem.ReservedQuantity -= orderItem.Quantity;
                            await _inventoryRepository.UpdateAsync(inventoryItem);
                        }
                    }
                }
                break;
            case "completed":
                // Finalize inventory changes - remove from reserved and total
                if (order.OrderItems != null)
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var inventoryItem = await _inventoryRepository.GetByIdAsync(orderItem.InventoryItemId);
                        if (inventoryItem != null)
                        {
                            inventoryItem.ReservedQuantity -= orderItem.Quantity;
                            inventoryItem.TotalQuantity -= orderItem.Quantity;
                            await _inventoryRepository.UpdateAsync(inventoryItem);
                        }
                    }
                }
                break;
        }
    }
}