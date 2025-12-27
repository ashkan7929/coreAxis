namespace CoreAxis.SharedKernel.Observability.Audit;

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? EventType { get; set; }
    public string? Module { get; set; }
    public string? Resource { get; set; }
    public string? OrderId { get; set; }
    public string? TxId { get; set; }
    public string? Severity { get; set; }
    public string? Details { get; set; }
}
