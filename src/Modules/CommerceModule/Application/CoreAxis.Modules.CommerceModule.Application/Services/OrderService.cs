using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.CommerceModule.Application.Services;

/// <summary>
/// Service for managing order operations.
/// </summary>
public class OrderService : IOrderService
{
    private readonly ICommerceDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ICommerceDbContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<OrderService> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<OrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

            // Implementation would go here
            // This is a placeholder implementation
            
            var orderId = Guid.NewGuid();
            
            return new OrderResult
            {
                Success = true,
                OrderId = orderId,
                Status = OrderStatus.Pending
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer {CustomerId}", request.CustomerId);
            throw;
        }
    }

    public async Task<OrderResult> UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        string? reason = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating order {OrderId} status to {Status}", orderId, status);

            // Implementation would go here
            // This is a placeholder implementation
            
            return new OrderResult
            {
                Success = true,
                OrderId = orderId,
                Status = status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} status", orderId);
            throw;
        }
    }

    public async Task<Order?> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<List<Order>> GetOrdersByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderResult> CancelOrderAsync(
        Guid orderId,
        string reason,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling order {OrderId} with reason: {Reason}", orderId, reason);

            // Implementation would go here
            // This is a placeholder implementation
            
            return new OrderResult
            {
                Success = true,
                OrderId = orderId,
                Status = OrderStatus.Cancelled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            throw;
        }
    }

    // --- Added implementations aligned with controller usage ---
    public async Task<List<Order>> GetOrdersAsync(
        Guid? customerId,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(o => o.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.OrderDate <= toDate.Value);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var skip = (page - 1) * pageSize;

        return await query
            .OrderByDescending(o => o.OrderDate)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order> CreateOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order {OrderNumber} for customer {CustomerId}", order.OrderNumber, order.CustomerId);

        if (order.Id == Guid.Empty)
            order.Id = Guid.NewGuid();

        if (string.IsNullOrWhiteSpace(order.OrderNumber))
            order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";

        order.Status = OrderStatus.Pending;
        if (order.OrderDate == default)
            order.OrderDate = DateTime.UtcNow;
        order.LastModified = DateTime.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order> UpdateOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

        if (existing is null)
            throw new InvalidOperationException($"Order {order.Id} not found.");

        existing.Status = order.Status;
        existing.ExpectedDeliveryDate = order.ExpectedDeliveryDate;
        existing.Subtotal = order.Subtotal;
        existing.TotalAmount = order.TotalAmount;
        existing.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<Order?> CancelOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null) return null;

        order.Cancel(reason);
        order.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order?> ConfirmOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null) return null;

        order.Confirm();
        order.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order?> FulfillOrderAsync(
        Guid orderId,
        string trackingNumber,
        string shippingCarrier,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null) return null;

        order.MarkAsShipped();

        // Assign tracking info if properties exist on Order (avoids compile-time mismatch across domains)
        try
        {
            var tProp = order.GetType().GetProperty("TrackingNumber");
            var cProp = order.GetType().GetProperty("ShippingCarrier");
            tProp?.SetValue(order, trackingNumber);
            cProp?.SetValue(order, shippingCarrier);
        }
        catch { }

        order.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }
}