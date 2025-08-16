using CoreAxis.EventBus;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles PriceLocked integration events to update order status and price information.
/// This handler processes PriceLocked events and updates the corresponding order.
/// </summary>
public class PriceLockedIntegrationEventHandler : IIntegrationEventHandler<PriceLocked>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PriceLockedIntegrationEventHandler> _logger;

    public PriceLockedIntegrationEventHandler(
        IOrderRepository orderRepository,
        ILogger<PriceLockedIntegrationEventHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the PriceLocked integration event by updating the order status and price information.
    /// </summary>
    /// <param name="event">The PriceLocked integration event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(PriceLocked @event)
    {
        try
        {
            _logger.LogInformation("Processing PriceLocked event for Order {OrderId} with price {LockedPrice}", 
                @event.OrderId, @event.LockedPrice);

            // Load order with for update to prevent race conditions
            var order = await _orderRepository.GetByIdAsync(@event.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for PriceLocked event", @event.OrderId);
                return;
            }

            // Check if order is in correct status for price locking
            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning("Order {OrderId} is not in pending status. Current status: {Status}", @event.OrderId, order.Status);
                return;
            }

            // Update order with locked price information
            var expiryDuration = @event.ExpiresAt - @event.LockedAt;
            
            order.SetPriceLock(@event.LockedPrice, @event.ExpiresAt);

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully updated Order {OrderId} with locked price {LockedPrice}, expires at {ExpiresAt}", 
                @event.OrderId, @event.LockedPrice, @event.ExpiresAt);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error processing PriceLocked event for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}