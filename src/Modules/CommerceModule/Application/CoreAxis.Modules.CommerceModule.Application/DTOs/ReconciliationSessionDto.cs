namespace CoreAxis.Modules.CommerceModule.Application.DTOs;

public class ReconciliationSessionDto
{
    public Guid Id { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTransactions { get; set; }
    public int ReconciledTransactions { get; set; }
    public int UnreconciledTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReconciledAmount { get; set; }
    public decimal UnreconciledAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ReconciliationEntryDto> Entries { get; set; } = new();
}