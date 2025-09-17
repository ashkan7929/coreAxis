using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Inventory;

public record GetInventoryItemByIdQuery(Guid Id) : IRequest<InventoryItemDto?>;

public class GetInventoryItemByIdQueryHandler : IRequestHandler<GetInventoryItemByIdQuery, InventoryItemDto?>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<GetInventoryItemByIdQueryHandler> _logger;

    public GetInventoryItemByIdQueryHandler(
        IInventoryRepository inventoryRepository,
        ILogger<GetInventoryItemByIdQueryHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<InventoryItemDto?> Handle(GetInventoryItemByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var inventoryItem = await _inventoryRepository.GetByIdWithLedgerAsync(request.Id);
            if (inventoryItem == null)
            {
                _logger.LogWarning("Inventory item with ID {InventoryItemId} not found", request.Id);
                return null;
            }

            var inventoryItemDto = new InventoryItemDto
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
                LedgerEntries = inventoryItem.LedgerEntries?.Select(le => new InventoryLedgerDto
                {
                    Id = le.Id,
                    InventoryItemId = le.InventoryItemId,
                    TransactionType = le.TransactionType,
                    Quantity = le.Quantity,
                    UnitPrice = le.UnitPrice,
                    Currency = le.Currency,
                    Reference = le.Reference,
                    Notes = le.Notes,
                    TransactionDate = le.TransactionDate,
                    CreatedAt = le.CreatedAt
                }).ToList() ?? new List<InventoryLedgerDto>()
            };

            _logger.LogInformation("Retrieved inventory item with ID: {InventoryItemId}", request.Id);
            return inventoryItemDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item with ID: {InventoryItemId}", request.Id);
            throw;
        }
    }
}