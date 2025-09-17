using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly CommerceDbContext _context;
    private readonly ILogger<InventoryRepository> _logger;

    public InventoryRepository(CommerceDbContext context, ILogger<InventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.InventoryItems
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item with ID {Id}", id);
            throw;
        }
    }

    public async Task<InventoryItem?> GetByIdWithLedgerAsync(Guid id)
    {
        try
        {
            return await _context.InventoryItems
                .Include(x => x.LedgerEntries.OrderByDescending(l => l.CreatedAt))
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item with ledger for ID {Id}", id);
            throw;
        }
    }

    public async Task<InventoryItem?> GetBySKUAsync(string sku)
    {
        try
        {
            return await _context.InventoryItems
                .FirstOrDefaultAsync(x => x.SKU == sku);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item with SKU {SKU}", sku);
            throw;
        }
    }

    public async Task<(List<InventoryItem> Items, int TotalCount)> GetInventoryItemsAsync(
        Guid? productId = null,
        string? sku = null,
        string? name = null,
        bool? isActive = null,
        bool? lowStock = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.InventoryItems.AsQueryable();

            if (productId.HasValue)
                query = query.Where(x => x.ProductId == productId.Value);

            if (!string.IsNullOrEmpty(sku))
                query = query.Where(x => x.SKU.Contains(sku));

            if (!string.IsNullOrEmpty(name))
                query = query.Where(x => x.Name.Contains(name));

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            if (lowStock.HasValue && lowStock.Value)
                query = query.Where(x => x.AvailableQuantity <= x.MinimumStockLevel);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory items");
            throw;
        }
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        try
        {
            return await _context.InventoryItems
                .Where(x => x.IsActive && x.AvailableQuantity <= x.MinimumStockLevel)
                .OrderBy(x => x.AvailableQuantity)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock items");
            throw;
        }
    }

    public async Task<InventoryItem> AddAsync(InventoryItem item)
    {
        try
        {
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Added inventory item with ID {Id}", item.Id);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding inventory item");
            throw;
        }
    }

    public async Task<InventoryItem> UpdateAsync(InventoryItem item)
    {
        try
        {
            _context.InventoryItems.Update(item);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated inventory item with ID {Id}", item.Id);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item with ID {Id}", item.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return false;

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted inventory item with ID {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.InventoryItems.AnyAsync(x => x.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of inventory item with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> SKUExistsAsync(string sku, Guid? excludeId = null)
    {
        try
        {
            var query = _context.InventoryItems.Where(x => x.SKU == sku);
            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SKU existence for {SKU}", sku);
            throw;
        }
    }

    public async Task<bool> ReserveQuantityAsync(Guid id, int quantity)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null || item.AvailableQuantity < quantity)
                return false;

            item.ReservedQuantity += quantity;
            item.AvailableQuantity -= quantity;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Reserved {Quantity} units for inventory item {Id}", quantity, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving quantity for inventory item {Id}", id);
            throw;
        }
    }

    public async Task<bool> ReleaseReservedQuantityAsync(Guid id, int quantity)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null || item.ReservedQuantity < quantity)
                return false;

            item.ReservedQuantity -= quantity;
            item.AvailableQuantity += quantity;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Released {Quantity} reserved units for inventory item {Id}", quantity, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reserved quantity for inventory item {Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateQuantityAsync(Guid id, int newQuantity, string reason, string? referenceId = null)
    {
        try
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return false;

            var oldQuantity = item.Quantity;
            var quantityChange = newQuantity - oldQuantity;

            item.Quantity = newQuantity;
            item.AvailableQuantity = newQuantity - item.ReservedQuantity;
            item.UpdatedAt = DateTime.UtcNow;

            // Create ledger entry
            var ledgerEntry = new InventoryLedger
            {
                Id = Guid.NewGuid(),
                InventoryItemId = id,
                TransactionType = quantityChange > 0 ? "Stock In" : "Stock Out",
                Quantity = Math.Abs(quantityChange),
                UnitPrice = item.UnitPrice,
                TotalValue = Math.Abs(quantityChange) * item.UnitPrice,
                BalanceAfter = newQuantity,
                ReferenceId = referenceId,
                ReferenceType = "Manual Adjustment",
                Notes = reason,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.InventoryLedgers.Add(ledgerEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quantity for inventory item {Id} from {OldQuantity} to {NewQuantity}", 
                id, oldQuantity, newQuantity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quantity for inventory item {Id}", id);
            throw;
        }
    }

    public async Task<InventoryLedger> AddLedgerEntryAsync(InventoryLedger entry)
    {
        try
        {
            _context.InventoryLedgers.Add(entry);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Added ledger entry for inventory item {InventoryItemId}", entry.InventoryItemId);
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding ledger entry for inventory item {InventoryItemId}", entry.InventoryItemId);
            throw;
        }
    }

    public async Task<List<InventoryLedger>> GetLedgerEntriesAsync(Guid inventoryItemId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            return await _context.InventoryLedgers
                .Where(x => x.InventoryItemId == inventoryItemId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ledger entries for inventory item {InventoryItemId}", inventoryItemId);
            throw;
        }
    }
}