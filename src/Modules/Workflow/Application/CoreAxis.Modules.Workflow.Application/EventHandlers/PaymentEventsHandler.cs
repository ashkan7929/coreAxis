using CoreAxis.EventBus;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.Workflow.Application.EventHandlers;

public class PaymentEventsHandler : IIntegrationEventHandler<PaymentConfirmed>, IIntegrationEventHandler<PaymentFailed>
{
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ILogger<PaymentEventsHandler> _logger;

    public PaymentEventsHandler(
        IWorkflowExecutor workflowExecutor,
        ILogger<PaymentEventsHandler> logger)
    {
        _workflowExecutor = workflowExecutor;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentConfirmed @event)
    {
        _logger.LogInformation("Handling PaymentConfirmed for Order {OrderId}", @event.OrderId);

        var payload = new Dictionary<string, object>
        {
            { "paymentId", @event.PaymentId },
            { "transactionId", @event.TransactionId },
            { "amount", @event.Amount },
            { "currency", @event.Currency },
            { "status", "Paid" }
        };

        // Signal the workflow using the order ID as the correlation ID
        await _workflowExecutor.SignalByCorrelationAsync(
            @event.OrderId.ToString(), 
            "PaymentConfirmed", 
            payload);
    }

    public async Task HandleAsync(PaymentFailed @event)
    {
        _logger.LogInformation("Handling PaymentFailed for Order {OrderId}. Reason: {Reason}", @event.OrderId, @event.Reason);

        var payload = new Dictionary<string, object>
        {
            { "paymentId", @event.PaymentId },
            { "reason", @event.Reason },
            { "status", "Failed" }
        };

        // Signal the workflow using the order ID as the correlation ID
        await _workflowExecutor.SignalByCorrelationAsync(
            @event.OrderId.ToString(), 
            "PaymentFailed", 
            payload);
    }
}
