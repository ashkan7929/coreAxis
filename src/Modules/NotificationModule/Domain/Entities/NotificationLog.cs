using CoreAxis.Modules.NotificationModule.Domain.Enums;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.NotificationModule.Domain.Entities;

public class NotificationLog : EntityBase
{
    public string Recipient { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Sent, Failed
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
    public Guid? RelatedEntityId { get; set; } // e.g., TaskId, PaymentId
}
