using CoreAxis.SharedKernel;

namespace CoreAxis.SharedKernel.Outbox;

public class OutboxMessage : EntityBase
{
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedOn { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public int MaxRetries { get; private set; } = 3;
    public DateTime? NextRetryAt { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    private OutboxMessage() { } // For EF Core

    public OutboxMessage(string type, string content, Guid correlationId, Guid? causationId = null, string tenantId = "default", int maxRetries = 3)
    {
        Type = type;
        Content = content;
        OccurredOn = DateTime.UtcNow;
        CorrelationId = correlationId;
        CausationId = causationId;
        TenantId = tenantId;
        MaxRetries = maxRetries;
        CreatedOn = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedOn = DateTime.UtcNow;
        Error = null;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        
        if (RetryCount < MaxRetries)
        {
            // Exponential backoff: 2^retryCount minutes
            var delayMinutes = Math.Pow(2, RetryCount);
            NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        
        LastModifiedOn = DateTime.UtcNow;
    }

    public bool IsProcessed => ProcessedOn.HasValue;
    public bool CanRetry => RetryCount < MaxRetries && (!NextRetryAt.HasValue || NextRetryAt <= DateTime.UtcNow);
    public bool HasFailed => !string.IsNullOrEmpty(Error);
    public bool IsMaxRetriesReached => RetryCount >= MaxRetries;
}