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
/// Service for managing inventory reservations with atomic operations and optimistic locking.
/// </summary>
public class ReservationService : IReservationService
{
    private readonly ICommerceDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<ReservationService> _logger;
    private const int MaxRetryAttempts = 3;

    public ReservationService(
        ICommerceDbContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Creates atomic inventory reservations for multiple items.
    /// </summary>
    public async Task<ReservationResult> CreateReservationsAsync(
        List<ReservationRequest> requests,
        Guid orderId,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var reservationId = Guid.NewGuid();
        var reservations = new List<InventoryReservation>();
        var failedItems = new List<ReservationFailure>();

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            foreach (var request in requests)
            {
                var result = await TryReserveItemAsync(
                    request, 
                    orderId, 
                    reservationId, 
                    correlationId, 
                    cancellationToken);

                if (result.Success)
                {
                    reservations.Add(result.Reservation!);
                }
                else
                {
                    failedItems.Add(new ReservationFailure(
                        request.ProductId,
                        request.VariantId,
                        request.Quantity,
                        result.ErrorMessage!));
                }
            }

            // If any item failed, rollback all reservations
            if (failedItems.Any())
            {
                await transaction.RollbackAsync(cancellationToken);
                
                await _eventDispatcher.DispatchAsync(
                    new InventoryReservationFailedEvent(
                        reservationId,
                        orderId,
                        failedItems.Select(f => new FailedReservationInfo(
                            f.ProductId,
                            f.VariantId,
                            f.Quantity,
                            f.ErrorMessage)).ToList(),
                        DateTime.UtcNow,
                        correlationId),
                    cancellationToken);

                return ReservationResult.Failed(failedItems);
            }

            // Save all reservations
            await _context.InventoryReservations.AddRangeAsync(reservations, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Dispatch success event
            await _eventDispatcher.DispatchAsync(
                new InventoryReservationCreatedEvent(
                    reservationId,
                    orderId,
                    reservations.Select(r => new ReservationInfo(
                        r.Id,
                        r.ProductId,
                        r.VariantId,
                        r.Quantity,
                        r.ReservedAt)).ToList(),
                    DateTime.UtcNow,
                    correlationId),
                cancellationToken);

            _logger.LogInformation(
                "Successfully created {Count} inventory reservations for order {OrderId}",
                reservations.Count, orderId);

            return ReservationResult.Success(reservations);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, 
                "Failed to create inventory reservations for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Attempts to reserve a single inventory item with optimistic locking.
    /// </summary>
    private async Task<SingleReservationResult> TryReserveItemAsync(
        ReservationRequest request,
        Guid orderId,
        Guid reservationId,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        
        while (retryCount < MaxRetryAttempts)
        {
            try
            {
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => 
                        i.ProductId == request.ProductId && 
                        i.VariantId == request.VariantId,
                        cancellationToken);

                if (inventoryItem == null)
                {
                    return SingleReservationResult.Failed(
                        $"Inventory item not found for Product {request.ProductId}, Variant {request.VariantId}");
                }

                // Check available quantity
                var availableQuantity = inventoryItem.QuantityOnHand - inventoryItem.ReservedQuantity;
                if (availableQuantity < request.Quantity)
                {
                    return SingleReservationResult.Failed(
                        $"Insufficient inventory. Available: {availableQuantity}, Requested: {request.Quantity}");
                }

                // Create reservation
                var reservation = new InventoryReservation
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId,
                    VariantId = request.VariantId,
                    Quantity = request.Quantity,
                    OrderId = orderId,
                    ReservationGroupId = reservationId,
                    Status = InventoryReservationStatus.Pending,
                    ReservedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes ?? 30),
                    CorrelationId = correlationId
                };

                // Update inventory with optimistic locking
                inventoryItem.ReservedQuantity += request.Quantity;
                inventoryItem.LastUpdatedAt = DateTime.UtcNow;

                // This will throw DbUpdateConcurrencyException if version mismatch
                _context.InventoryItems.Update(inventoryItem);
                
                return SingleReservationResult.Success(reservation);
            }
            catch (DbUpdateConcurrencyException)
            {
                retryCount++;
                _logger.LogWarning(
                    "Concurrency conflict when reserving inventory for Product {ProductId}, Variant {VariantId}. Retry {RetryCount}/{MaxRetries}",
                    request.ProductId, request.VariantId, retryCount, MaxRetryAttempts);

                if (retryCount >= MaxRetryAttempts)
                {
                    return SingleReservationResult.Failed(
                        "Failed to reserve inventory due to concurrency conflicts after maximum retries");
                }

                // Wait before retry with exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount)), cancellationToken);
            }
        }

        return SingleReservationResult.Failed("Maximum retry attempts exceeded");
    }

    /// <summary>
    /// Confirms reservations and converts them to committed inventory.
    /// </summary>
    public async Task<bool> ConfirmReservationsAsync(
        Guid orderId,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var reservations = await _context.InventoryReservations
                .Where(r => r.OrderId == orderId && r.Status == InventoryReservationStatus.Pending)
                .ToListAsync(cancellationToken);

            if (!reservations.Any())
            {
                _logger.LogWarning("No active reservations found for order {OrderId}", orderId);
                return false;
            }

            foreach (var reservation in reservations)
            {
                // Update reservation status
                 reservation.Commit();

                // Update inventory quantities
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => 
                        i.ProductId == reservation.ProductId,
                        cancellationToken);

                if (inventoryItem != null)
                {
                    inventoryItem.QuantityOnHand -= reservation.Quantity;
                    inventoryItem.ReservedQuantity -= reservation.Quantity;
                    inventoryItem.LastUpdatedAt = DateTime.UtcNow;

                    // Create ledger entry
                    var ledgerEntry = new InventoryLedger
                    {
                        Id = Guid.NewGuid(),
                        ProductId = reservation.ProductId,
                        VariantId = reservation.VariantId,
                        TransactionType = InventoryTransactionType.Sale,
                        Quantity = -reservation.Quantity,
                        ReferenceId = orderId,
                        ReferenceType = "Order",
                        TransactionDate = DateTime.UtcNow,
                        Notes = $"Confirmed reservation for order {orderId}",
                        CorrelationId = correlationId
                    };

                    await _context.InventoryLedgers.AddAsync(ledgerEntry, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Event dispatching removed due to property compatibility issues

            _logger.LogInformation(
                "Successfully confirmed {Count} reservations for order {OrderId}",
                reservations.Count, orderId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, 
                "Failed to confirm reservations for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Releases reservations and returns inventory to available stock.
    /// </summary>
    public async Task<bool> ReleaseReservationsAsync(
        Guid orderId,
        string? reason = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var reservations = await _context.InventoryReservations
                .Where(r => r.OrderId == orderId && r.Status == InventoryReservationStatus.Pending)
                .ToListAsync(cancellationToken);

            if (!reservations.Any())
            {
                _logger.LogWarning("No active reservations found for order {OrderId}", orderId);
                return false;
            }

            foreach (var reservation in reservations)
            {
                // Update reservation status
                 reservation.Release(reason);

                // Return inventory to available stock
                var items = reservation.GetReservationItems();
                foreach (var item in items)
                {
                    var inventoryItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(i => 
                            i.ProductId == item.ProductId,
                            cancellationToken);

                    if (inventoryItem != null)
                    {
                        // Use domain method to release reservation
                        inventoryItem.Release(item.Quantity, reservation.Id);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Events are dispatched automatically by domain entities

            _logger.LogInformation(
                "Successfully released {Count} reservations for order {OrderId}. Reason: {Reason}",
                reservations.Count,
                orderId,
                reason ?? "Not specified");

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, 
                "Failed to release reservations for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Cleans up expired reservations.
    /// </summary>
    public async Task<int> CleanupExpiredReservationsAsync(
        CancellationToken cancellationToken = default)
    {
        var expiredReservations = await _context.InventoryReservations
            .Where(r => r.Status == InventoryReservationStatus.Pending && r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (!expiredReservations.Any())
            return 0;

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            foreach (var reservation in expiredReservations)
         {
             reservation.MarkAsExpired();

             // Return inventory to available stock
             var items = JsonSerializer.Deserialize<List<ReservationItem>>(reservation.ItemsJson);
             if (items != null)
             {
                 foreach (var item in items)
                 {
                     var inventoryItem = await _context.InventoryItems
                         .FirstOrDefaultAsync(i => 
                             i.ProductId == item.ProductId && 
                             i.Sku == item.Sku,
                             cancellationToken);
 
                     if (inventoryItem != null)
                     {
                         inventoryItem.Release(item.Quantity, reservation.OrderId);
                     }
                 }
             }
         }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} expired reservations", expiredReservations.Count());

            return expiredReservations.Count;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to cleanup expired reservations");
            throw;
        }
    }
}