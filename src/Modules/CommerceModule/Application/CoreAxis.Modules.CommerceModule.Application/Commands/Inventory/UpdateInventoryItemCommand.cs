using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Inventory;

public record UpdateInventoryItemCommand(
    Guid Id,
    string Name,
    string Description,
    decimal AvailableQuantity,
    decimal UnitPrice,
    string Currency,
    bool IsActive
) : IRequest<InventoryItemDto>;

public class UpdateInventoryItemCommandHandler : IRequestHandler<UpdateInventoryItemCommand, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<UpdateInventoryItemCommandHandler> _logger;

    public UpdateInventoryItemCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<UpdateInventoryItemCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<InventoryItemDto> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var inventoryItem = await _inventoryRepository.GetByIdAsync(request.Id);
            if (inventoryItem == null)
            {
                throw new InvalidOperationException($"Inventory item with ID {request.Id} not found.");
            }

            inventoryItem.Name = request.Name;
            inventoryItem.Description = request.Description;
            inventoryItem.AvailableQuantity = request.AvailableQuantity;
            inventoryItem.TotalQuantity = request.AvailableQuantity + inventoryItem.ReservedQuantity;
            inventoryItem.UnitPrice = request.UnitPrice;
            inventoryItem.Currency = request.Currency;
            inventoryItem.IsActive = request.IsActive;
            inventoryItem.UpdatedAt = DateTime.UtcNow;

            await _inventoryRepository.UpdateAsync(inventoryItem);
            await _inventoryRepository.SaveChangesAsync();

            _logger.LogInformation("Inventory item updated successfully with ID: {InventoryItemId}", inventoryItem.Id);

            return new InventoryItemDto
            {
                Id = inventoryItem.Id,
                AssetCode = inventoryItem.AssetCode,
                Name = inventoryItem.Name,
                Description = inventoryItem.Description,
                AvailableQuantity = inventoryItem.AvailableQuantity,
                ReservedQuantity = inventoryItem.ReservedQuantity,
                TotalQuantity = inventoryItem.TotalQuantity,
                UnitPrice = inventoryItem.UnitPrice,
                Currency = inventoryItem.Currency,
                IsActive = inventoryItem.IsActive,
                CreatedAt = inventoryItem.CreatedAt,
                UpdatedAt = inventoryItem.UpdatedAt,
                LedgerEntries = new List<InventoryLedgerDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item with ID: {InventoryItemId}", request.Id);
            throw;
        }
    }
}