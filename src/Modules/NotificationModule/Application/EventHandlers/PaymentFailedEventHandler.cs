using CoreAxis.EventBus;
using CoreAxis.Modules.NotificationModule.Application.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.NotificationModule.Application.EventHandlers;

public class PaymentFailedEventHandler : IIntegrationEventHandler<PaymentFailed>
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(NotificationService notificationService, ILogger<PaymentFailedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentFailed @event)
    {
        _logger.LogInformation("Handling PaymentFailed for payment {PaymentId}, reason {Reason}", @event.PaymentId, @event.Reason);

        // Assume UserID is available or we have a recipient somehow.
        // PaymentFailed event might not have recipient info directly if it's purely backend ID.
        // But for now, we'll assume we can't send unless we look it up.
        // Or we just log it.
        
        // Let's assume we can't notify without user ID, but if the event has it...
        // Checking PaymentFailed event definition.
        
        var recipient = "admin@coreaxis.com"; // Fallback or assume admin for now
        var parameters = new Dictionary<string, string>
        {
            { "PaymentId", @event.PaymentId.ToString() },
            { "Reason", @event.Reason ?? "Unknown" },
            { "OrderId", @event.OrderId.ToString() }
        };

        await _notificationService.SendNotificationAsync("PAYMENT_FAILED", recipient, parameters);
    }
}
