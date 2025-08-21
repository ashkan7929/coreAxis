using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Interface for inventory service operations.
/// </summary>
public interface IInventoryService
{
    Task<InventoryResult> UpdateStockAsync(
        Guid productId,
        int quantity,
        InventoryLedgerReason reason,
        string? notes = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetInventoryItemAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<List<InventoryItem>> GetLowStockItemsAsync(
        int threshold = 10,
        CancellationToken cancellationToken = default);

    Task<bool> IsStockAvailableAsync(
        Guid productId,
        int requestedQuantity,
        CancellationToken cancellationToken = default);

    Task<InventoryResult> CreateInventoryItemAsync(
        CreateInventoryItemRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating inventory items.
/// </summary>
public class CreateInventoryItemRequest
{
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public int InitialQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public string? Location { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result model for inventory operations.
/// </summary>
public class InventoryResult
{
    public bool Success { get; set; }
    public Guid? ProductId { get; set; }
    public int? NewQuantity { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}