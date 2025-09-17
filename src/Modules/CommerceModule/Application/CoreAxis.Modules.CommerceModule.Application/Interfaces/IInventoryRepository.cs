using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id);
    Task<InventoryItem?> GetByIdWithLedgerAsync(Guid id);
    Task<InventoryItem?> GetByAssetCodeAsync(string assetCode);
    Task<(List<InventoryItem> Items, int TotalCount)> GetInventoryItemsAsync(
        string? assetCode = null,
        string? name = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<InventoryItem> AddAsync(InventoryItem inventoryItem);
    Task<InventoryItem> UpdateAsync(InventoryItem inventoryItem);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ReserveQuantityAsync(Guid inventoryItemId, int quantity);
    Task<bool> ReleaseReservedQuantityAsync(Guid inventoryItemId, int quantity);
    Task<bool> UpdateAvailableQuantityAsync(Guid inventoryItemId, int quantityChange);
    Task<InventoryLedger> AddLedgerEntryAsync(InventoryLedger ledgerEntry);
    Task<List<InventoryLedger>> GetLedgerEntriesAsync(Guid inventoryItemId, int pageNumber = 1, int pageSize = 10);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> AssetCodeExistsAsync(string assetCode, Guid? excludeId = null);
}