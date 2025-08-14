using CoreAxis.SharedKernel.Ports;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CoreAxis.Adapters.Stubs;

public class WorkflowClientStub : IWorkflowClient
{
    private readonly IPriceProvider _priceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WorkflowClientStub> _logger;
    private readonly ConcurrentDictionary<Guid, WorkflowState> _workflowStates = new();
    private readonly ConcurrentDictionary<string, PriceLock> _priceLocks = new();

    public WorkflowClientStub(IPriceProvider priceProvider, IEventBus eventBus, ILogger<WorkflowClientStub> logger)
    {
        _priceProvider = priceProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<WorkflowResult> StartAsync(string definitionId, object context, CancellationToken cancellationToken = default)
    {
        var workflowId = Guid.NewGuid();
        var state = new WorkflowState(workflowId, definitionId, "Running", context);
        _workflowStates[workflowId] = state;

        _logger.LogInformation("Started workflow {WorkflowId} with definition {DefinitionId}", workflowId, definitionId);

        // Handle different workflow types
        switch (definitionId.ToLower())
        {
            case "quote-workflow":
                await HandleQuoteWorkflow(state, cancellationToken);
                break;
            case "price-lock-workflow":
                await HandlePriceLockWorkflow(state, cancellationToken);
                break;
            default:
                state.Status = "Failed";
                state.Error = $"Unknown workflow definition: {definitionId}";
                break;
        }

        return new WorkflowResult(workflowId, state.Status, state.Result, state.Error);
    }

    public async Task<WorkflowResult> SignalAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received signal {EventName} with payload {Payload}", eventName, JsonSerializer.Serialize(payload));

        // Handle OrderPlaced event
        if (eventName == "OrderPlaced" && payload is OrderPlaced orderPlaced)
        {
            await HandleOrderPlaced(orderPlaced, cancellationToken);
        }

        return new WorkflowResult(Guid.NewGuid(), "Completed");
    }

    public async Task<WorkflowResult> GetWorkflowStatusAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        if (_workflowStates.TryGetValue(workflowId, out var state))
        {
            return new WorkflowResult(workflowId, state.Status, state.Result, state.Error);
        }

        return new WorkflowResult(workflowId, "NotFound", error: "Workflow not found");
    }

    private async Task HandleOrderPlaced(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        try
        {
            // Get price quote
            var priceContext = new PriceContext(orderPlaced.TenantId, orderPlaced.UserId, orderPlaced.CorrelationId);
            var quote = await _priceProvider.GetQuoteAsync(orderPlaced.AssetCode, orderPlaced.Quantity, priceContext, cancellationToken);

            // Publish PriceQuoted event
            var priceQuoted = new PriceQuoted(
                orderId: orderPlaced.OrderId,
                assetCode: orderPlaced.AssetCode,
                quantity: orderPlaced.Quantity,
                price: quote.Price,
                timestamp: quote.Timestamp,
                providerId: quote.ProviderId,
                expiresInSeconds: quote.ExpiresInSeconds,
                tenantId: orderPlaced.TenantId,
                correlationId: orderPlaced.CorrelationId,
                causationId: orderPlaced.Id
            );

            await _eventBus.PublishAsync(priceQuoted);
            _logger.LogInformation("Published PriceQuoted event for order {OrderId}", orderPlaced.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderPlaced event for order {OrderId}", orderPlaced.OrderId);
        }
    }

    private async Task HandleQuoteWorkflow(WorkflowState state, CancellationToken cancellationToken)
    {
        // This is a placeholder for quote workflow logic
        await Task.Delay(100, cancellationToken);
        state.Status = "Completed";
        state.Result = "Quote workflow completed";
    }

    private async Task HandlePriceLockWorkflow(WorkflowState state, CancellationToken cancellationToken)
    {
        try
        {
            var lockRequest = JsonSerializer.Deserialize<PriceLockRequest>(JsonSerializer.Serialize(state.Context));
            if (lockRequest == null)
            {
                state.Status = "Failed";
                state.Error = "Invalid lock request";
                return;
            }

            var lockId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddSeconds(120); // 120 seconds expiry
            
            var priceLock = new PriceLock
            {
                LockId = lockId,
                AssetCode = lockRequest.AssetCode,
                Price = lockRequest.Price,
                Quantity = lockRequest.Quantity,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _priceLocks[lockId] = priceLock;

            // Publish PriceLocked event
            var priceLocked = new PriceLocked(
                orderId: lockRequest.OrderId,
                assetCode: lockRequest.AssetCode,
                quantity: lockRequest.Quantity,
                lockedPrice: lockRequest.Price,
                lockedAt: DateTime.UtcNow,
                expiresAt: expiresAt,
                tenantId: lockRequest.TenantId,
                correlationId: lockRequest.CorrelationId
            );

            await _eventBus.PublishAsync(priceLocked);
            
            state.Status = "Completed";
            state.Result = priceLock;
            
            _logger.LogInformation("Created price lock {LockId} for {AssetCode} at price {Price}", 
                lockId, lockRequest.AssetCode, lockRequest.Price);
        }
        catch (Exception ex)
        {
            state.Status = "Failed";
            state.Error = ex.Message;
            _logger.LogError(ex, "Error in price lock workflow");
        }
    }

    private class WorkflowState
    {
        public Guid WorkflowId { get; }
        public string DefinitionId { get; }
        public string Status { get; set; }
        public object Context { get; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; }

        public WorkflowState(Guid workflowId, string definitionId, string status, object context)
        {
            WorkflowId = workflowId;
            DefinitionId = definitionId;
            Status = status;
            Context = context;
            CreatedAt = DateTime.UtcNow;
        }
    }

    private class PriceLock
    {
        public string LockId { get; set; } = string.Empty;
        public string AssetCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class PriceLockRequest
    {
        public Guid OrderId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
    }
}