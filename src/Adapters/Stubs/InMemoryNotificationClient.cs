using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CoreAxis.Adapters.Stubs;

public class InMemoryNotificationClient : INotificationClient
{
    private readonly ILogger<InMemoryNotificationClient> _logger;
    private readonly ConcurrentQueue<NotificationRecord> _sentNotifications = new();

    public InMemoryNotificationClient(ILogger<InMemoryNotificationClient> logger)
    {
        _logger = logger;
    }

    public async Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate sending

        var notificationId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;

        // Store notification record
        var record = new NotificationRecord
        {
            Id = notificationId,
            Type = request.Type,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Content = request.Content,
            SentAt = timestamp,
            Status = "Sent"
        };
        
        _sentNotifications.Enqueue(record);

        var result = new NotificationResult(
            notificationId: notificationId,
            isSuccess: true,
            status: "Sent",
            timestamp: timestamp,
            message: "Notification sent successfully"
        );

        _logger.LogInformation("Notification sent - ID: {NotificationId}, Type: {Type}, Recipient: {Recipient}, Subject: {Subject}", 
            notificationId, request.Type, request.Recipient, request.Subject);

        return result;
    }

    public async Task<NotificationStatusResult> GetStatusAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken); // Simulate lookup

        // Find notification in our queue (this is inefficient but fine for a stub)
        var notification = _sentNotifications.FirstOrDefault(n => n.Id == notificationId);

        if (notification != null)
        {
            var result = new NotificationStatusResult(
                notificationId: notificationId,
                status: notification.Status,
                sentAt: notification.SentAt,
                deliveredAt: notification.SentAt.AddSeconds(5), // Simulate delivery delay
                isDelivered: true,
                attempts: 1
            );

            _logger.LogInformation("Notification status retrieved for ID: {NotificationId}, Status: {Status}", 
                notificationId, notification.Status);

            return result;
        }
        else
        {
            var result = new NotificationStatusResult(
                notificationId: notificationId,
                status: "NotFound",
                sentAt: null,
                deliveredAt: null,
                isDelivered: false,
                attempts: 0
            );

            _logger.LogWarning("Notification not found for ID: {NotificationId}", notificationId);
            return result;
        }
    }

    // Helper method to get all sent notifications (for testing/debugging)
    public IEnumerable<NotificationRecord> GetAllSentNotifications()
    {
        return _sentNotifications.ToArray();
    }

    public class NotificationRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}