namespace CoreAxis.SharedKernel.Observability.Audit;

public interface IAuditStore
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task<List<AuditEntry>> QueryAsync(
        string? correlationId = null,
        string? userId = null,
        string? orderId = null,
        string? txId = null,
        string? eventType = null,
        string? severity = null,
        int page = 1,
        int size = 50,
        CancellationToken cancellationToken = default);
}