using CoreAxis.Modules.NotificationModule.Domain.Enums;

namespace CoreAxis.Modules.NotificationModule.Application.Contracts;

public interface INotificationProvider
{
    NotificationChannel Channel { get; }
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
