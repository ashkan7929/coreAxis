using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CoreAxis.Modules.CommerceModule.Api.Controllers;

/// <summary>
/// Controller for managing inventory operations
/// </summary>
[ApiController]
[Route("api/v1/commerce/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IReservationService _reservationService;
    private readonly ILogger<InventoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryController"/> class
    /// </summary>
    /// <param name="inventoryService">The inventory service</param>
    /// <param name="reservationService">The reservation service</param>
    /// <param name="logger">The logger</param>
    public InventoryController(
        IInventoryService inventoryService,
        IReservationService reservationService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _reservationService = reservationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all inventory items with optional filtering
    /// </summary>
    /// <param name="productId">Optional product ID filter</param>
    /// <param name="sku">Optional SKU filter</param>
    /// <param name="location">Optional location filter</param>
    /// <param name="lowStock">Filter for low stock items only</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>List of inventory items</returns>
    [HttpGet]
    [HasPermission("inventory", "read")]
    [ProducesResponseType(typeof(IEnumerable<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetInventoryItems(
        [FromQuery] Guid? productId = null,
        [FromQuery] string? sku = null,
        [FromQuery] string? location = null,
        [FromQuery] bool lowStock = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            _logger.LogInformation("Getting inventory items with filters: ProductId={ProductId}, SKU={Sku}, Location={Location}, LowStock={LowStock}, Page={Page}, PageSize={PageSize}",
                productId, sku, location, lowStock, page, pageSize);

            var items = await _inventoryService.GetInventoryItemsAsync(
                productId, sku, location, lowStock, page, pageSize);

            var dtos = items.Select(MapToDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory items");
            return StatusCode(500, "An error occurred while retrieving inventory items");
        }
    }

    /// <summary>
    /// Gets a specific inventory item by ID
    /// </summary>
    /// <param name="id">The inventory item ID</param>
    /// <returns>The inventory item</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("inventory", "read")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryItemDto>> GetInventoryItem(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting inventory item with ID: {Id}", id);

            var item = await _inventoryService.GetInventoryItemByIdAsync(id);
            if (item == null)
            {
                return NotFound($"Inventory item with ID {id} not found");
            }

            return Ok(MapToDto(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the inventory item");
        }
    }

    /// <summary>
    /// Creates a new inventory item
    /// </summary>
    /// <param name="createDto">The inventory item creation data</param>
    /// <returns>The created inventory item</returns>
    [HttpPost]
    [HasPermission("inventory", "create")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryItemDto>> CreateInventoryItem([FromBody] CreateInventoryItemDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new inventory item for product: {ProductId}", createDto.ProductId);

            var inventoryItem = InventoryItem.Create(
                createDto.ProductId,
                createDto.Sku,
                null, // LocationId - will be handled separately if needed
                createDto.QuantityOnHand,
                createDto.ReorderLevel,
                true // IsTracked
            );

            var createdItem = await _inventoryService.CreateInventoryItemAsync(inventoryItem);

            _logger.LogInformation("Successfully created inventory item with ID: {Id}", createdItem.Id);

            return CreatedAtAction(
                nameof(GetInventoryItem),
                new { id = createdItem.Id },
                MapToDto(createdItem));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating inventory item");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return StatusCode(500, "An error occurred while creating the inventory item");
        }
    }

    /// <summary>
    /// Updates an existing inventory item
    /// </summary>
    /// <param name="id">The inventory item ID</param>
    /// <param name="updateDto">The inventory item update data</param>
    /// <returns>The updated inventory item</returns>
    [HttpPut("{id:guid}")]
    [HasPermission("inventory", "update")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryItemDto>> UpdateInventoryItem(Guid id, [FromBody] UpdateInventoryItemDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating inventory item with ID: {Id}", id);

            var existingItem = await _inventoryService.GetInventoryItemByIdAsync(id);
            if (existingItem == null)
            {
                return NotFound($"Inventory item with ID {id} not found");
            }

            // Update only provided fields
            if (updateDto.QuantityOnHand.HasValue)
                existingItem.QuantityOnHand = updateDto.QuantityOnHand.Value;
            if (updateDto.ReorderLevel.HasValue)
                existingItem.ReorderLevel = updateDto.ReorderLevel.Value;
            if (updateDto.MaxStockLevel.HasValue)
                existingItem.MaxStockLevel = updateDto.MaxStockLevel.Value;
            if (!string.IsNullOrEmpty(updateDto.Location))
                existingItem.Location = updateDto.Location;

            existingItem.LastUpdated = DateTime.UtcNow;
            existingItem.QuantityAvailable = existingItem.QuantityOnHand - existingItem.QuantityReserved;

            var updatedItem = await _inventoryService.UpdateInventoryItemAsync(existingItem);

            _logger.LogInformation("Successfully updated inventory item with ID: {Id}", id);

            return Ok(MapToDto(updatedItem));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating inventory item with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the inventory item");
        }
    }

    /// <summary>
    /// Deletes an inventory item
    /// </summary>
    /// <param name="id">The inventory item ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [HasPermission("inventory", "delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteInventoryItem(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting inventory item with ID: {Id}", id);

            var existingItem = await _inventoryService.GetInventoryItemByIdAsync(id);
            if (existingItem == null)
            {
                return NotFound($"Inventory item with ID {id} not found");
            }

            // Check if item has reservations
            if (existingItem.QuantityReserved > 0)
            {
                return BadRequest("Cannot delete inventory item with active reservations");
            }

            await _inventoryService.DeleteInventoryItemAsync(id);

            _logger.LogInformation("Successfully deleted inventory item with ID: {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the inventory item");
        }
    }

    /// <summary>
    /// Reserves inventory for a customer
    /// </summary>
    /// <param name="reserveDto">The reservation request data</param>
    /// <returns>The reservation result</returns>
    [HttpPost("reserve")]
    [HasPermission("inventory", "reserve")]
    [ProducesResponseType(typeof(InventoryReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryReservationDto>> ReserveInventory([FromBody] ReserveInventoryDto reserveDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} reserving {Quantity} units of product {ProductId} for customer {CustomerId}",
                userId, reserveDto.Quantity, reserveDto.ProductId, reserveDto.CustomerId);

            var reservationDuration = TimeSpan.FromMinutes(reserveDto.ReservationDurationMinutes);
            var result = await _reservationService.ReserveInventoryAsync(
                reserveDto.ProductId,
                reserveDto.Quantity,
                reserveDto.CustomerId,
                reservationDuration);

            var responseDto = new InventoryReservationDto
            {
                ReservationId = result.ReservationId,
                ProductId = reserveDto.ProductId,
                CustomerId = reserveDto.CustomerId,
                Quantity = reserveDto.Quantity,
                ExpiresAt = DateTime.UtcNow.Add(reservationDuration),
                Success = result.Success,
                ErrorMessage = result.Success ? null : "Insufficient inventory available"
            };

            if (result.Success)
            {
                _logger.LogInformation("Successfully reserved inventory. Reservation ID: {ReservationId}", result.ReservationId);
                return Ok(responseDto);
            }
            else
            {
                _logger.LogWarning("Failed to reserve inventory for product {ProductId}", reserveDto.ProductId);
                return BadRequest(responseDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory");
            return StatusCode(500, "An error occurred while reserving inventory");
        }
    }

    /// <summary>
    /// Releases a reservation
    /// </summary>
    /// <param name="reservationId">The reservation ID to release</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("reservations/{reservationId:guid}")]
    [HasPermission("inventory", "reserve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ReleaseReservation(Guid reservationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} releasing reservation {ReservationId}", userId, reservationId);

            await _reservationService.ReleaseReservationAsync(reservationId);

            _logger.LogInformation("Successfully released reservation {ReservationId}", reservationId);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reservation ID: {ReservationId}", reservationId);
            return NotFound($"Reservation with ID {reservationId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reservation {ReservationId}", reservationId);
            return StatusCode(500, "An error occurred while releasing the reservation");
        }
    }

    #region Private Methods

    private static InventoryItemDto MapToDto(InventoryItem item)
    {
        return new InventoryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            Sku = item.Sku,
            QuantityOnHand = (int)item.OnHand,
            QuantityReserved = (int)item.Reserved,
            QuantityAvailable = (int)item.Available,
            ReorderLevel = (int)item.ReorderThreshold,
            MaxStockLevel = 0, // Not available in domain model
            Location = item.LocationId?.ToString() ?? string.Empty,
            LastUpdated = item.LastModifiedOn ?? item.CreatedOn
        };
    }

    #endregion
}