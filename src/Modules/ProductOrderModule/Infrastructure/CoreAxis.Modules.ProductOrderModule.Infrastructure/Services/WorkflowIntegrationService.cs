using CoreAxis.SharedKernel.Ports;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;

/// <summary>
/// Service responsible for integrating with workflow systems for order processing.
/// Handles the Quote→Lock workflow for order placement.
/// </summary>
public class WorkflowIntegrationService : IWorkflowIntegrationService
{
    private readonly IWorkflowClient _workflowClient;
    private readonly ILogger<WorkflowIntegrationService> _logger;

    public WorkflowIntegrationService(
        IWorkflowClient workflowClient,
        ILogger<WorkflowIntegrationService> logger)
    {
        _workflowClient = workflowClient;
        _logger = logger;
    }

    /// <summary>
    /// Initiates the Quote→Lock workflow for a placed order.
    /// </summary>
    /// <param name="orderPlacedEvent">The OrderPlaced integration event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    public async Task<WorkflowResult> InitiateQuoteLockWorkflowAsync(
        OrderPlaced orderPlacedEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Initiating Quote→Lock workflow for Order {OrderId}",
                orderPlacedEvent.OrderId);

            // Prepare workflow context with order details
            var workflowContext = new
            {
                OrderId = orderPlacedEvent.OrderId,
                UserId = orderPlacedEvent.UserId,
                AssetCode = orderPlacedEvent.AssetCode,
                Quantity = orderPlacedEvent.Quantity,
                TenantId = orderPlacedEvent.TenantId,
                PlacedAt = orderPlacedEvent.CreationDate,
                CorrelationId = orderPlacedEvent.CorrelationId,
                Metadata = orderPlacedEvent.Metadata
            };

            // Start the quote-workflow which will handle the Quote→Lock process
            var result = await _workflowClient.StartAsync(
                "quote-workflow",
                workflowContext,
                cancellationToken);

            _logger.LogInformation(
                "Successfully initiated Quote→Lock workflow for Order {OrderId}. Workflow ID: {WorkflowId}",
                orderPlacedEvent.OrderId,
                result.WorkflowId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initiate Quote→Lock workflow for Order {OrderId}",
                orderPlacedEvent.OrderId);
            throw;
        }
    }

    /// <summary>
    /// Signals an existing workflow with order-related events.
    /// </summary>
    /// <param name="signalName">The signal name.</param>
    /// <param name="signalData">The signal data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task SignalWorkflowAsync(
        string signalName,
        object signalData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Signaling workflow with signal {SignalName}",
                signalName);

            await _workflowClient.SignalAsync(signalName, signalData, cancellationToken);

            _logger.LogInformation(
                "Successfully signaled workflow with signal {SignalName}",
                signalName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to signal workflow with signal {SignalName}",
                signalName);
            throw;
        }
    }

    /// <summary>
    /// Gets the status of a workflow instance.
    /// </summary>
    /// <param name="workflowId">The workflow instance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result containing status and details.</returns>
    public async Task<WorkflowResult> GetWorkflowStatusAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting status for workflow {WorkflowId}", workflowId);
            
            return await _workflowClient.GetWorkflowStatusAsync(workflowId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get status for workflow {WorkflowId}",
                workflowId);
            throw;
        }
    }
}

/// <summary>
/// Interface for workflow integration service.
/// </summary>
public interface IWorkflowIntegrationService
{
    /// <summary>
    /// Initiates the Quote→Lock workflow for a placed order.
    /// </summary>
    /// <param name="orderPlacedEvent">The order placed event containing order details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result.</returns>
    Task<WorkflowResult> InitiateQuoteLockWorkflowAsync(
        OrderPlaced orderPlacedEvent, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals an existing workflow with order-related events.
    /// </summary>
    /// <param name="signalName">The signal name.</param>
    /// <param name="signalData">The signal data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SignalWorkflowAsync(
        string signalName,
        object signalData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a workflow instance.
    /// </summary>
    /// <param name="workflowId">The workflow instance ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow result containing status and details.</returns>
    Task<WorkflowResult> GetWorkflowStatusAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);
}