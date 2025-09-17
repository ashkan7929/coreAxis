using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Inventory;

public record CreateInventoryItemCommand(
    string AssetCode,
    string Name,
    string Description,
    decimal AvailableQuantity,
    decimal UnitPrice,
    string Currency
) : IRequest<InventoryItemDto>;

public class CreateInventoryItemCommandHandler : IRequestHandler<CreateInventoryItemCommand, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<CreateInventoryItemCommandHandler> _logger;

    public CreateInventoryItemCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<CreateInventoryItemCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<InventoryItemDto> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var inventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                AssetCode = request.AssetCode,
                Name = request.Name,
                Description = request.Description,
                AvailableQuantity = request.AvailableQuantity,
                ReservedQuantity = 0,
                TotalQuantity = request.AvailableQuantity,
                UnitPrice = request.UnitPrice,
                Currency = request.Currency,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _inventoryRepository.AddAsync(inventoryItem);
            await _inventoryRepository.SaveChangesAsync();

            _logger.LogInformation("Inventory item created successfully with ID: {InventoryItemId}", inventoryItem.Id);

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
            _logger.LogError(ex, "Error creating inventory item with asset code: {AssetCode}", request.AssetCode);
            throw;
        }
    }
}