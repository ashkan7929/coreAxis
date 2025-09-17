namespace CoreAxis.Modules.CommerceModule.Application.DTOs;

public class InventoryLedgerDto
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}