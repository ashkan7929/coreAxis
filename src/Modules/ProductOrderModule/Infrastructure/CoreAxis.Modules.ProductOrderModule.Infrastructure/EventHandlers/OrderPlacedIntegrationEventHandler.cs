using CoreAxis.EventBus;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles OrderPlaced integration events to initiate workflow processing.
/// This handler processes events from the outbox and triggers the Quote→Lock workflow.
/// </summary>
public class OrderPlacedIntegrationEventHandler : IIntegrationEventHandler<OrderPlaced>
{
    private readonly IWorkflowIntegrationService _workflowIntegrationService;
    private readonly ILogger<OrderPlacedIntegrationEventHandler> _logger;

    public OrderPlacedIntegrationEventHandler(
        IWorkflowIntegrationService workflowIntegrationService,
        ILogger<OrderPlacedIntegrationEventHandler> logger)
    {
        _workflowIntegrationService = workflowIntegrationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the OrderPlaced integration event by initiating the Quote→Lock workflow.
    /// </summary>
    /// <param name="event">The OrderPlaced integration event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(OrderPlaced @event)
    {
        try
        {
            _logger.LogInformation("Processing OrderPlaced event for Order {OrderId}", @event.OrderId);
            
            // Initiate the Quote→Lock workflow
            var workflowResult = await _workflowIntegrationService.InitiateQuoteLockWorkflowAsync(@event);
            
            if (workflowResult.IsSuccess)
            {
                _logger.LogInformation("Successfully initiated Quote→Lock workflow {WorkflowId} for Order {OrderId}", 
                    workflowResult.WorkflowId, @event.OrderId);
            }
            else
            {
                _logger.LogError("Failed to initiate Quote→Lock workflow for Order {OrderId}: {Error}", 
                    @event.OrderId, workflowResult.Error);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderPlaced event for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}