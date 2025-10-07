using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using MediatR;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
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
            var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {request.OrderId} not found.");
            }

            var previousStatus = order.Status;

            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
                throw new InvalidOperationException($"Invalid order status: {request.Status}");

            switch (newStatus)
            {
                case OrderStatus.Confirmed:
                    order.Confirm();
                    break;
                case OrderStatus.Shipped:
                    order.MarkAsShipped();
                    break;
                case OrderStatus.Delivered:
                    order.MarkAsDelivered();
                    break;
                case OrderStatus.Cancelled:
                    order.Cancel();
                    break;
                default:
                    order.Status = newStatus;
                    break;
            }

            order.LastModifiedOn = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Notes))
            {
                order.SpecialInstructions = string.IsNullOrEmpty(order.SpecialInstructions)
                    ? request.Notes
                    : $"{order.SpecialInstructions}\n{request.Notes}";
            }

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Order status updated successfully. OrderId: {OrderId}, Status: {Status}", 
                order.Id, request.Status);

            return new OrderDto
            {
                Id = order.Id,
                UserId = order.CustomerId,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                FinalAmount = order.TotalAmount,
                Notes = order.SpecialInstructions,
                OrderDate = order.OrderDate,
                ShippingDate = null,
                DeliveryDate = order.DeliveredAt,
                CreatedAt = order.CreatedOn,
                UpdatedAt = order.LastModifiedOn,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    InventoryItemId = Guid.Empty,
                    AssetCode = oi.ProductSku,
                    ItemName = oi.ProductName,
                    Quantity = (decimal)oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice,
                    Currency = order.Currency,
                    DiscountAmount = oi.DiscountAmount,
                    Notes = null,
                    CreatedAt = oi.CreatedOn,
                    UpdatedAt = oi.LastModifiedOn
                }).ToList() ?? new List<OrderItemDto>(),
                Payment = order.Payments?
                    .OrderByDescending(p => p.ProcessedAt ?? p.CreatedOn)
                    .Select(payment => new PaymentDto
                    {
                        Id = payment.Id,
                        OrderId = payment.OrderId,
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        PaymentMethod = payment.Method.ToString(),
                        Status = payment.Status.ToString(),
                        TransactionId = payment.TransactionId,
                        GatewayResponse = payment.GatewayReference,
                        ProcessedAt = payment.ProcessedAt,
                        FailureReason = payment.FailureReason,
                        CreatedAt = payment.CreatedOn,
                        UpdatedAt = payment.LastModifiedOn
                    })
                    .FirstOrDefault(),
                AppliedDiscounts = new List<DiscountRuleDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status. OrderId: {OrderId}", request.OrderId);
            throw;
        }
    }

    // Status-specific inventory logic is handled within domain methods or separate workflows.
}