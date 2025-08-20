using CoreAxis.SharedKernel;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using System.Text.Json;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a reservation of inventory items for an order.
/// </summary>
public class InventoryReservation : EntityBase
{
    /// <summary>
    /// Gets the order identifier this reservation is for.
    /// </summary>
    public Guid OrderId { get; private set; }
    
    /// <summary>
    /// Gets the user identifier who made the reservation.
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// Gets the JSON representation of reserved items.
    /// </summary>
    public string ItemsJson { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the current status of the reservation.
    /// </summary>
    public InventoryReservationStatus Status { get; private set; }
    
    /// <summary>
    /// Gets the expiration time of this reservation.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// Gets the correlation identifier for tracking.
    /// </summary>
    public string CorrelationId { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the idempotency key for duplicate prevention.
    /// </summary>
    public string? IdempotencyKey { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether this reservation has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt && Status == InventoryReservationStatus.Pending;
    
    /// <summary>
    /// Gets a value indicating whether this reservation can be committed.
    /// </summary>
    public bool CanCommit => Status == InventoryReservationStatus.Pending && !IsExpired;
    
    /// <summary>
    /// Gets a value indicating whether this reservation can be released.
    /// </summary>
    public bool CanRelease => Status == InventoryReservationStatus.Pending;

    // Private constructor for EF Core
    private InventoryReservation() { }

    /// <summary>
    /// Creates a new inventory reservation.
    /// </summary>
    /// <param name="orderId">The order identifier</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="items">The items to reserve</param>
    /// <param name="expiresAt">When the reservation expires</param>
    /// <param name="correlationId">The correlation identifier</param>
    /// <param name="idempotencyKey">The idempotency key</param>
    /// <returns>A new InventoryReservation instance</returns>
    public static InventoryReservation Create(
        Guid orderId,
        Guid userId,
        IEnumerable<ReservationItem> items,
        DateTime expiresAt,
        string correlationId,
        string? idempotencyKey = null)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty.", nameof(correlationId));
        
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration time must be in the future.", nameof(expiresAt));
        
        var itemsList = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (!itemsList.Any())
            throw new ArgumentException("Items cannot be empty.", nameof(items));

        var reservation = new InventoryReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            ItemsJson = JsonSerializer.Serialize(itemsList),
            Status = InventoryReservationStatus.Pending,
            ExpiresAt = expiresAt,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            CreatedOn = DateTime.UtcNow,
            IsActive = true
        };

        reservation.AddDomainEvent(new InventoryReservationCreatedEvent(
            reservation.Id,
            orderId,
            userId,
            itemsList,
            expiresAt,
            correlationId));

        return reservation;
    }

    /// <summary>
    /// Commits the reservation, finalizing the inventory allocation.
    /// </summary>
    public void Commit()
    {
        if (!CanCommit)
            throw new InvalidOperationException($"Cannot commit reservation in {Status} status or expired reservation.");
        
        Status = InventoryReservationStatus.Committed;
        LastModifiedOn = DateTime.UtcNow;
        
        var items = GetReservationItems();
        AddDomainEvent(new InventoryReservationCommittedEvent(
            Id,
            OrderId,
            UserId,
            items,
            CorrelationId));
    }

    /// <summary>
    /// Releases the reservation, making the inventory available again.
    /// </summary>
    /// <param name="reason">The reason for releasing the reservation</param>
    public void Release(string reason = "Manual release")
    {
        if (!CanRelease)
            throw new InvalidOperationException($"Cannot release reservation in {Status} status.");
        
        Status = InventoryReservationStatus.Released;
        LastModifiedOn = DateTime.UtcNow;
        
        var items = GetReservationItems();
        AddDomainEvent(new InventoryReservationReleasedEvent(
            Id,
            OrderId,
            UserId,
            items,
            reason,
            CorrelationId));
    }

    /// <summary>
    /// Marks the reservation as expired.
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status != InventoryReservationStatus.Pending)
            throw new InvalidOperationException($"Cannot expire reservation in {Status} status.");
        
        Status = InventoryReservationStatus.Expired;
        LastModifiedOn = DateTime.UtcNow;
        
        var items = GetReservationItems();
        AddDomainEvent(new InventoryReservationExpiredEvent(
            Id,
            OrderId,
            UserId,
            items,
            CorrelationId));
    }

    /// <summary>
    /// Extends the expiration time of the reservation.
    /// </summary>
    /// <param name="newExpiresAt">The new expiration time</param>
    public void ExtendExpiration(DateTime newExpiresAt)
    {
        if (Status != InventoryReservationStatus.Pending)
            throw new InvalidOperationException($"Cannot extend reservation in {Status} status.");
        
        if (newExpiresAt <= DateTime.UtcNow)
            throw new ArgumentException("New expiration time must be in the future.", nameof(newExpiresAt));
        
        if (newExpiresAt <= ExpiresAt)
            throw new ArgumentException("New expiration time must be later than current expiration.", nameof(newExpiresAt));
        
        ExpiresAt = newExpiresAt;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new InventoryReservationExtendedEvent(
            Id,
            OrderId,
            newExpiresAt,
            CorrelationId));
    }

    /// <summary>
    /// Gets the reservation items from the JSON representation.
    /// </summary>
    /// <returns>The list of reservation items</returns>
    public List<ReservationItem> GetReservationItems()
    {
        if (string.IsNullOrWhiteSpace(ItemsJson))
            return new List<ReservationItem>();
        
        try
        {
            return JsonSerializer.Deserialize<List<ReservationItem>>(ItemsJson) ?? new List<ReservationItem>();
        }
        catch (JsonException)
        {
            return new List<ReservationItem>();
        }
    }
}

/// <summary>
/// Represents an item in an inventory reservation.
/// </summary>
public class ReservationItem
{
    /// <summary>
    /// Gets or sets the product identifier.
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Gets or sets the SKU.
    /// </summary>
    public string Sku { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the quantity to reserve.
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Gets or sets the location identifier.
    /// </summary>
    public Guid? LocationId { get; set; }
}