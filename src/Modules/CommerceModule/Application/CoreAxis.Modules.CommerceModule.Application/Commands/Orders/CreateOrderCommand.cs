using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Orders;

public record CreateOrderItemRequest(
    Guid InventoryItemId,
    decimal Quantity,
    string? Notes = null
);

public record CreateOrderCommand(
    Guid UserId,
    List<CreateOrderItemRequest> OrderItems,
    string? Notes = null
) : IRequest<OrderDto>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                OrderNumber = GenerateOrderNumber(),
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;
            string currency = "USD"; // Default currency

            foreach (var itemRequest in request.OrderItems)
            {
                var inventoryItem = await _inventoryRepository.GetByIdAsync(itemRequest.InventoryItemId);
                if (inventoryItem == null)
                {
                    throw new InvalidOperationException($"Inventory item with ID {itemRequest.InventoryItemId} not found.");
                }

                if (inventoryItem.AvailableQuantity < itemRequest.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient quantity for item {inventoryItem.Name}. Available: {inventoryItem.AvailableQuantity}, Requested: {itemRequest.Quantity}");
                }

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    InventoryItemId = itemRequest.InventoryItemId,
                    AssetCode = inventoryItem.AssetCode,
                    ItemName = inventoryItem.Name,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = inventoryItem.UnitPrice,
                    TotalPrice = itemRequest.Quantity * inventoryItem.UnitPrice,
                    Currency = inventoryItem.Currency,
                    Notes = itemRequest.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                orderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
                currency = inventoryItem.Currency; // Use the currency from inventory items

                // Reserve inventory
                inventoryItem.AvailableQuantity -= itemRequest.Quantity;
                inventoryItem.ReservedQuantity += itemRequest.Quantity;
                await _inventoryRepository.UpdateAsync(inventoryItem);
            }

            order.TotalAmount = totalAmount;
            order.FinalAmount = totalAmount; // Before any discounts or taxes
            order.Currency = currency;

            await _orderRepository.AddAsync(order);
            
            foreach (var orderItem in orderItems)
            {
                await _orderRepository.AddOrderItemAsync(orderItem);
            }

            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Order created successfully with ID: {OrderId} for User: {UserId}", order.Id, request.UserId);

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
                OrderItems = orderItems.Select(oi => new OrderItemDto
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
                }).ToList(),
                Payment = null,
                AppliedDiscounts = new List<DiscountRuleDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user: {UserId}", request.UserId);
            throw;
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}