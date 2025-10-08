using System.Text.Json;
using System.Threading.Tasks;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.Workflow.Infrastructure.EventHandlers;

/// <summary>
/// Starts the post-finalize workflow when an OrderFinalized event is received.
/// </summary>
public class OrderFinalizedStartPostFinalizeHandler : IIntegrationEventHandler<OrderFinalized>
{
    private readonly IWorkflowClient _workflowClient;
    private readonly ILogger<OrderFinalizedStartPostFinalizeHandler> _logger;

    public OrderFinalizedStartPostFinalizeHandler(
        IWorkflowClient workflowClient,
        ILogger<OrderFinalizedStartPostFinalizeHandler> logger)
    {
        _workflowClient = workflowClient;
        _logger = logger;
    }

    public async Task HandleAsync(OrderFinalized @event)
    {
        var context = new
        {
            @event.OrderId,
            @event.UserId,
            @event.TotalAmount,
            @event.Currency,
            @event.FinalizedAt,
            @event.TenantId,
            CorrelationId = @event.CorrelationId
        };

        var json = JsonSerializer.Serialize(context);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        _logger.LogInformation("Starting post-finalize workflow for Order {OrderId}", @event.OrderId);
        var result = await _workflowClient.StartAsync("post-finalize-workflow", jsonElement);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Post-finalize workflow {WorkflowId} started for Order {OrderId}",
                result.WorkflowId, @event.OrderId);
        }
        else
        {
            _logger.LogWarning(
                "Failed to start post-finalize workflow for Order {OrderId}: {Error}",
                @event.OrderId, result.Error);
        }
    }
}