using CoreAxis.EventBus;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles PriceLocked integration events to update order status and price information.
/// This handler processes PriceLocked events and updates the corresponding order.
/// </summary>
public class PriceLockedIntegrationEventHandler : IIntegrationEventHandler<PriceLocked>
{
    private readonly ProductOrderDbContext _context;
    private readonly ILogger<PriceLockedIntegrationEventHandler> _logger;

    public PriceLockedIntegrationEventHandler(
        ProductOrderDbContext context,
        ILogger<PriceLockedIntegrationEventHandler> logger)
    {
        _context = context;
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
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == @event.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for PriceLocked event", @event.OrderId);
                return;
            }

            // Check if order is in correct status for price locking
            if (order.Status != CoreAxis.Modules.ProductOrderModule.Domain.Enums.OrderStatus.Pending)
            {
                _logger.LogWarning("Order {OrderId} is not in pending status. Current status: {Status}", @event.OrderId, order.Status);
                return;
            }

            // Update order with locked price information
            var lockedPrice = Money.Create(@event.LockedPrice, "USD"); // Default currency, should be configurable
            var expiryDuration = @event.ExpiresAt - @event.LockedAt;
            
            order.LockPrice(lockedPrice, expiryDuration);

            await _context.SaveChangesAsync();

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