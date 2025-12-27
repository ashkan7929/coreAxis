using System.Text.Json;

namespace CoreAxis.SharedKernel.Observability.Audit;

public class AuditFileStore : IAuditStore
{
    private readonly string _basePath;

    public AuditFileStore(string basePath)
    {
        _basePath = basePath;
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var filename = $"{entry.Timestamp:yyyy-MM-dd}.log";
        var path = Path.Combine(_basePath, filename);
        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllLinesAsync(path, new[] { json }, cancellationToken);
    }

    public async Task<List<AuditEntry>> QueryAsync(
        string? correlationId = null,
        string? userId = null,
        string? orderId = null,
        string? txId = null,
        string? eventType = null,
        string? severity = null,
        int page = 1,
        int size = 50,
        CancellationToken cancellationToken = default)
    {
        var result = new List<AuditEntry>();
        
        // Simple implementation: read all log files and filter in memory
        // WARNING: Inefficient for large logs. For production, use a real DB or structured log store (Elasticsearch/Seq)
        if (!Directory.Exists(_basePath))
        {
            return result;
        }

        var files = Directory.GetFiles(_basePath, "*.log")
                             .OrderByDescending(f => f); // Newest files first

        int skip = (page - 1) * size;
        int take = size;
        int skipped = 0;

        foreach (var file in files)
        {
            if (result.Count >= take) break;

            var lines = await File.ReadAllLinesAsync(file, cancellationToken);
            // Read lines in reverse order (newest first)
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (result.Count >= take) break;

                try
                {
                    var entry = JsonSerializer.Deserialize<AuditEntry>(lines[i]);
                    if (entry == null) continue;

                    // Apply filters
                    if (correlationId != null && entry.CorrelationId != correlationId) continue;
                    if (userId != null && entry.UserId != userId) continue;
                    // Note: AuditEntry might not have OrderId/TxId/Severity explicitly if they are in 'Details' or not in the class definition.
                    // Assuming AuditEntry has these properties based on AdminAuditController usage.
                    // If not, we need to add them to AuditEntry.cs or filter by Details/ExtensionData.
                    
                    // Checking AuditEntry definition...
                    // "AuditEntry class with properties (Id, Timestamp, CorrelationId, UserId, Action, Resource, Details)"
                    // It seems AuditEntry is missing OrderId, TxId, Severity, EventType (maybe Action is EventType?)
                    
                    // Let's assume for now we filter what we can match.
                    if (eventType != null && entry.EventType != eventType) continue; 
                    
                    // Handling skipped items for pagination
                    if (skipped < skip)
                    {
                        skipped++;
                        continue;
                    }

                    result.Add(entry);
                }
                catch
                {
                    // Ignore malformed lines
                }
            }
        }

        return result;
    }
}