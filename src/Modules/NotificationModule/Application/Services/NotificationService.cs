using CoreAxis.Modules.NotificationModule.Application.Contracts;
using CoreAxis.Modules.NotificationModule.Domain.Entities;
using CoreAxis.Modules.NotificationModule.Domain.Enums;
using CoreAxis.Modules.NotificationModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.NotificationModule.Application.Services;

public class NotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IEnumerable<INotificationProvider> providers,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _providers = providers;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string templateKey, string recipient, Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        var template = await _context.Templates.FirstOrDefaultAsync(t => t.Key == templateKey, cancellationToken);
        if (template == null)
        {
            _logger.LogError("Notification template not found: {TemplateKey}", templateKey);
            return;
        }

        var provider = _providers.FirstOrDefault(p => p.Channel == template.Channel);
        if (provider == null)
        {
            _logger.LogError("No provider found for channel: {Channel}", template.Channel);
            return;
        }

        var subject = ReplaceParameters(template.SubjectTemplate, parameters);
        var body = ReplaceParameters(template.BodyTemplate, parameters);

        try
        {
            await provider.SendAsync(recipient, subject, body, cancellationToken);
            await LogNotificationAsync(recipient, template.Channel, subject, body, "Sent", null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {Recipient}", recipient);
            await LogNotificationAsync(recipient, template.Channel, subject, body, "Failed", ex.Message, cancellationToken);
        }
    }

    private string ReplaceParameters(string template, Dictionary<string, string> parameters)
    {
        foreach (var param in parameters)
        {
            template = template.Replace($"{{{{{param.Key}}}}}", param.Value);
        }
        return template;
    }

    private async Task LogNotificationAsync(string recipient, NotificationChannel channel, string subject, string body, string status, string? error, CancellationToken cancellationToken)
    {
        var log = new NotificationLog
        {
            Recipient = recipient,
            Channel = channel,
            Subject = subject,
            Body = body,
            Status = status,
            ErrorMessage = error,
            SentAt = DateTime.UtcNow
        };

        _context.Logs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
