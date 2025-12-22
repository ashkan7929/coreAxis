using CoreAxis.Modules.NotificationModule.Application.Contracts;
using CoreAxis.Modules.NotificationModule.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.NotificationModule.Infrastructure.Providers;

public class EmailNotificationProvider : INotificationProvider
{
    private readonly ILogger<EmailNotificationProvider> _logger;

    public EmailNotificationProvider(ILogger<EmailNotificationProvider> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending EMAIL to {Recipient}. Subject: {Subject}. Body: {Body}", recipient, subject, body);
        return Task.CompletedTask;
    }
}
