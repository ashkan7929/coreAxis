using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.CommerceModule.Api.DTOs;

/// <summary>
/// Data transfer object for inventory item information
/// </summary>
public class InventoryItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the inventory item
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the SKU (Stock Keeping Unit)
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity on hand
    /// </summary>
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// Gets or sets the quantity reserved
    /// </summary>
    public int QuantityReserved { get; set; }

    /// <summary>
    /// Gets or sets the quantity available
    /// </summary>
    public int QuantityAvailable { get; set; }

    /// <summary>
    /// Gets or sets the reorder level
    /// </summary>
    public int ReorderLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level
    /// </summary>
    public int MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Data transfer object for creating a new inventory item
/// </summary>
public class CreateInventoryItemDto
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the SKU (Stock Keeping Unit)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial quantity on hand
    /// </summary>
    [Range(0, int.MaxValue)]
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// Gets or sets the reorder level
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the location
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for updating an inventory item
/// </summary>
public class UpdateInventoryItemDto
{
    /// <summary>
    /// Gets or sets the quantity on hand
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? QuantityOnHand { get; set; }

    /// <summary>
    /// Gets or sets the reorder level
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? ReorderLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the location
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Location { get; set; }
}

/// <summary>
/// Data transfer object for inventory reservation request
/// </summary>
public class ReserveInventoryDto
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity to reserve
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the reservation duration in minutes
    /// </summary>
    [Range(1, 1440)] // Max 24 hours
    public int ReservationDurationMinutes { get; set; } = 60;
}

/// <summary>
/// Data transfer object for inventory reservation response
/// </summary>
public class InventoryReservationDto
{
    /// <summary>
    /// Gets or sets the reservation identifier
    /// </summary>
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the reserved quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the reservation expiry time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the reservation is successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if reservation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}