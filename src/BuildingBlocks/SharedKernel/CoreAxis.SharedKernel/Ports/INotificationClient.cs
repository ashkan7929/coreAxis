namespace CoreAxis.SharedKernel.Ports;

public interface INotificationClient
{
    Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<NotificationStatusResult> GetStatusAsync(string notificationId, CancellationToken cancellationToken = default);
}

public class NotificationRequest
{
    public string Type { get; }
    public string Recipient { get; }
    public string Subject { get; }
    public string Content { get; }
    public Dictionary<string, object> Metadata { get; }

    public NotificationRequest(string type, string recipient, string subject, string content, Dictionary<string, object>? metadata = null)
    {
        Type = type;
        Recipient = recipient;
        Subject = subject;
        Content = content;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

public class NotificationResult
{
    public string NotificationId { get; }
    public bool IsSuccess { get; }
    public string Status { get; }
    public DateTime Timestamp { get; }
    public string Message { get; }

    public NotificationResult(string notificationId, bool isSuccess, string status, DateTime timestamp, string message)
    {
        NotificationId = notificationId;
        IsSuccess = isSuccess;
        Status = status;
        Timestamp = timestamp;
        Message = message;
    }
}

public class NotificationStatusResult
{
    public string NotificationId { get; }
    public string Status { get; }
    public DateTime? SentAt { get; }
    public DateTime? DeliveredAt { get; }
    public bool IsDelivered { get; }
    public int Attempts { get; }

    public NotificationStatusResult(string notificationId, string status, DateTime? sentAt, DateTime? deliveredAt, bool isDelivered, int attempts)
    {
        NotificationId = notificationId;
        Status = status;
        SentAt = sentAt;
        DeliveredAt = deliveredAt;
        IsDelivered = isDelivered;
        Attempts = attempts;
    }
}