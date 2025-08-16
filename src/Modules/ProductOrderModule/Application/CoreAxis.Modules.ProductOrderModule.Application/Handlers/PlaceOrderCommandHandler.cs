using CoreAxis.Modules.ProductOrderModule.Application.Commands;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

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
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required", nameof(request.UserId));
            
            if (string.IsNullOrWhiteSpace(request.AssetCode))
                throw new ArgumentException("AssetCode is required", nameof(request.AssetCode));
            
            if (request.TotalAmount <= 0)
                throw new ArgumentException("TotalAmount must be positive", nameof(request.TotalAmount));
            
            if (!request.OrderLines.Any())
                throw new ArgumentException("At least one order line is required", nameof(request.OrderLines));

            // Enhanced idempotency check with retry logic
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existingOrder = await GetExistingOrderWithRetryAsync(request.IdempotencyKey, cancellationToken);
                if (existingOrder != null)
                {
                    _logger.LogInformation("Idempotent request detected. Returning existing order {OrderId} for IdempotencyKey {IdempotencyKey}", 
                        existingOrder.Id, request.IdempotencyKey);
                    
                    return MapToDto(existingOrder);
                }
            }

            // Create order with transaction safety
            var order = await CreateOrderWithTransactionAsync(request, cancellationToken);
            
            _logger.LogInformation("Successfully created order {OrderId} for user {UserId} with IdempotencyKey {IdempotencyKey}", 
                order.Id, request.UserId, request.IdempotencyKey ?? "None");

            return MapToDto(order);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid request for PlaceOrder: {Message}. UserId: {UserId}, IdempotencyKey: {IdempotencyKey}", 
                ex.Message, request.UserId, request.IdempotencyKey);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business rule violation while placing order for user {UserId} with IdempotencyKey {IdempotencyKey}", 
                request.UserId, request.IdempotencyKey);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while placing order for user {UserId} with IdempotencyKey {IdempotencyKey}", 
                request.UserId, request.IdempotencyKey);
            throw;
        }
    }

    private async Task<Order?> GetExistingOrderWithRetryAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int delayMs = 100;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey);
                return existingOrder;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("Attempt {Attempt} failed to check idempotency key {IdempotencyKey}: {Message}. Retrying...", 
                    attempt, idempotencyKey, ex.Message);
                
                await Task.Delay(delayMs * attempt, cancellationToken);
            }
        }
        
        // Final attempt without catching exception
        return await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey);
    }

    private async Task<Order> CreateOrderWithTransactionAsync(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Create order lines with validation
        var orderLines = new List<OrderLine>();
        foreach (var orderLineDto in request.OrderLines)
        {
            if (string.IsNullOrWhiteSpace(orderLineDto.AssetCode))
                throw new ArgumentException($"AssetCode is required for order line");
            
            if (orderLineDto.Quantity <= 0)
                throw new ArgumentException($"Quantity must be positive for asset {orderLineDto.AssetCode}");
            
            if (orderLineDto.UnitPrice <= 0)
                throw new ArgumentException($"UnitPrice must be positive for asset {orderLineDto.AssetCode}");

            var orderLine = OrderLine.Create(
                AssetCode.Create(orderLineDto.AssetCode),
                orderLineDto.Quantity,
                orderLineDto.UnitPrice
            );
            orderLines.Add(orderLine);
        }

        // Validate total amount matches sum of order lines
        var calculatedTotal = orderLines.Sum(ol => ol.TotalPrice);
        if (Math.Abs(calculatedTotal - request.TotalAmount) > 0.00000001m)
        {
            _logger.LogWarning("TotalAmount mismatch: provided {ProvidedTotal}, calculated {CalculatedTotal}", 
                request.TotalAmount, calculatedTotal);
            throw new ArgumentException($"TotalAmount {request.TotalAmount} does not match calculated total {calculatedTotal}");
        }

        // Create order
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

        // Save with transaction safety
        try
        {
            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save order for user {UserId} with IdempotencyKey {IdempotencyKey}", 
                request.UserId, request.IdempotencyKey);
            
            // Check if this was a duplicate key violation due to race condition
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && 
                ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Detected race condition for IdempotencyKey {IdempotencyKey}. Attempting to retrieve existing order.", 
                    request.IdempotencyKey);
                
                var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
                if (existingOrder != null)
                {
                    _logger.LogInformation("Successfully retrieved existing order {OrderId} after race condition", existingOrder.Id);
                    return existingOrder;
                }
            }
            
            throw;
        }
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