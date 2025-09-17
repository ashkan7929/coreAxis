using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Inventory;

public record GetInventoryItemsQuery(
    string? AssetCode = null,
    string? Name = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<(List<InventoryItemDto> Items, int TotalCount)>;

public class GetInventoryItemsQueryHandler : IRequestHandler<GetInventoryItemsQuery, (List<InventoryItemDto> Items, int TotalCount)>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<GetInventoryItemsQueryHandler> _logger;

    public GetInventoryItemsQueryHandler(
        IInventoryRepository inventoryRepository,
        ILogger<GetInventoryItemsQueryHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<(List<InventoryItemDto> Items, int TotalCount)> Handle(GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (inventoryItems, totalCount) = await _inventoryRepository.GetInventoryItemsAsync(
                request.AssetCode,
                request.Name,
                request.IsActive,
                request.PageNumber,
                request.PageSize);

            var inventoryItemDtos = inventoryItems.Select(item => new InventoryItemDto
            {
                Id = item.Id,
                AssetCode = item.AssetCode,
                Name = item.Name,
                Description = item.Description,
                AvailableQuantity = item.AvailableQuantity,
                ReservedQuantity = item.ReservedQuantity,
                TotalQuantity = item.TotalQuantity,
                UnitPrice = item.UnitPrice,
                Currency = item.Currency,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                LedgerEntries = item.LedgerEntries?.Select(le => new InventoryLedgerDto
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
            }).ToList();

            _logger.LogInformation("Retrieved {Count} inventory items out of {TotalCount} total items", 
                inventoryItemDtos.Count, totalCount);

            return (inventoryItemDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory items");
            throw;
        }
    }
}