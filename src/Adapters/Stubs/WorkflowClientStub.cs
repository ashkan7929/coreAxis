using CoreAxis.SharedKernel.Ports;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
using System.IO;

namespace CoreAxis.Adapters.Stubs;

public class WorkflowClientStub : IWorkflowClient
{
    private readonly IPriceProvider _priceProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WorkflowClientStub> _logger;
    private readonly ConcurrentDictionary<Guid, WorkflowState> _workflowStates = new();
    private readonly ConcurrentDictionary<string, PriceLock> _priceLocks = new();
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _loggedSteps = new();
    private readonly string _storeRoot;

    public WorkflowClientStub(IPriceProvider priceProvider, IEventBus eventBus, ILogger<WorkflowClientStub> logger)
    {
        _priceProvider = priceProvider;
        _eventBus = eventBus;
        _logger = logger;
        _storeRoot = Path.Combine(AppContext.BaseDirectory, "App_Data", "workflows");
        Directory.CreateDirectory(_storeRoot);
    }

    public async Task<WorkflowResult> StartAsync(string definitionId, object context, int? version = null, CancellationToken cancellationToken = default)
    {
        var workflowId = Guid.NewGuid();
        var state = new WorkflowState(workflowId, definitionId, "Running", context);
        _workflowStates[workflowId] = state;

        _logger.LogInformation("Started workflow {WorkflowId} with definition {DefinitionId} (Version: {Version})", workflowId, definitionId, version);

        // Persist start
        AppendStepLog(workflowId, "workflow.start", "Running", new { definitionId, version });

        // Handle different workflow types
        switch (definitionId.ToLower())
        {
            case "quote-workflow":
                await HandleQuoteWorkflow(state, cancellationToken);
                break;
            case "price-lock-workflow":
                await HandlePriceLockWorkflow(state, cancellationToken);
                break;
            case "post-finalize-workflow":
                await HandlePostFinalizeWorkflow(state, cancellationToken);
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

        // Handle resume/cancel signals
        if (payload is JsonElement json)
        {
            if (eventName.Equals("Resume", StringComparison.OrdinalIgnoreCase) &&
                json.TryGetProperty("workflowId", out var wfProp))
            {
                var workflowId = wfProp.GetGuid();
                AppendStepLog(workflowId, "signal.resume", "Received");
                if (_workflowStates.TryGetValue(workflowId, out var state))
                {
                    state.Status = "Running";
                    AppendStepLog(workflowId, "resume.applied", "Running");
                    return new WorkflowResult(workflowId, state.Status, state.Result, state.Error);
                }
                return new WorkflowResult(workflowId, "NotFound", error: "Workflow not found");
            }

            if (eventName.Equals("Cancel", StringComparison.OrdinalIgnoreCase) &&
                json.TryGetProperty("workflowId", out var wfProp2))
            {
                var workflowId = wfProp2.GetGuid();
                AppendStepLog(workflowId, "signal.cancel", "Received");
                if (_workflowStates.TryGetValue(workflowId, out var state))
                {
                    state.Status = "Canceled";
                    AppendStepLog(workflowId, "cancel.applied", "Canceled");
                    return new WorkflowResult(workflowId, state.Status, state.Result, state.Error);
                }
                return new WorkflowResult(workflowId, "NotFound", error: "Workflow not found");
            }
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
        try
        {
            // Extract order context from workflow state
            var contextJson = JsonSerializer.Serialize(state.Context);
            var orderContext = JsonSerializer.Deserialize<Dictionary<string, object>>(contextJson);
            
            if (orderContext == null)
            {
                state.Status = "Failed";
                state.Error = "Invalid order context";
                return;
            }

            var orderId = Guid.Parse(orderContext["OrderId"].ToString()!);
            var assetCode = orderContext["AssetCode"].ToString()!;
            var quantity = Convert.ToDecimal(orderContext["Quantity"].ToString());
            var tenantId = orderContext["TenantId"].ToString()!;
            var correlationId = Guid.Parse(orderContext["CorrelationId"].ToString()!);

            _logger.LogInformation("Processing Quote→Lock workflow for Order {OrderId}, Asset {AssetCode}, Quantity {Quantity}", 
                orderId, assetCode, quantity);

            // Step 1: Get price quote using PriceProvider
             var priceContext = new CoreAxis.SharedKernel.Ports.PriceContext(
                 tenantId: tenantId,
                 userId: Guid.Parse(orderContext["UserId"].ToString()!),
                 correlationId: correlationId
             );

             var quote = await _priceProvider.GetQuoteAsync(assetCode, quantity, priceContext, cancellationToken);
             var price = quote.Price;
            
            _logger.LogInformation("Retrieved price {Price} for asset {AssetCode}", price, assetCode);

            // Step 2: Lock the price (simulate lock duration)
            var lockedAt = DateTime.UtcNow;
            var expiresAt = lockedAt.AddMinutes(15); // 15 minutes lock duration
            
            // Step 3: Publish PriceLocked event
            var priceLocked = new PriceLocked(
                orderId: orderId,
                assetCode: assetCode,
                quantity: quantity,
                lockedPrice: price,
                lockedAt: lockedAt,
                expiresAt: expiresAt,
                tenantId: tenantId,
                correlationId: correlationId
            );

            await _eventBus.PublishAsync(priceLocked);
            
            state.Status = "Completed";
            state.Result = new { Price = price, LockedAt = lockedAt, ExpiresAt = expiresAt };
            
            _logger.LogInformation("Successfully completed Quote→Lock workflow for Order {OrderId}. Price {Price} locked until {ExpiresAt}", 
                orderId, price, expiresAt);
        }
        catch (Exception ex)
        {
            state.Status = "Failed";
            state.Error = ex.Message;
            _logger.LogError(ex, "Error in quote workflow for workflow {WorkflowId}", state.WorkflowId);
        }
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

    private async Task HandlePostFinalizeWorkflow(WorkflowState state, CancellationToken cancellationToken)
    {
        var workflowId = state.WorkflowId;
        try
        {
            // Extract context
            var contextJson = JsonSerializer.Serialize(state.Context);
            var ctx = JsonSerializer.Deserialize<Dictionary<string, object>>(contextJson) ?? new();

            // ScriptTask: prepare summary
            var step1 = "post-finalize.script.prepare";
            if (!IsStepLogged(workflowId, step1))
            {
                AppendStepLog(workflowId, step1, "Started", ctx);
                var summary = new
                {
                    OrderId = ctx.GetValueOrDefault("OrderId"),
                    UserId = ctx.GetValueOrDefault("UserId"),
                    TotalAmount = ctx.GetValueOrDefault("TotalAmount"),
                    Currency = ctx.GetValueOrDefault("Currency"),
                    FinalizedAt = ctx.GetValueOrDefault("FinalizedAt"),
                    TenantId = ctx.GetValueOrDefault("TenantId")
                };
                AppendStepLog(workflowId, step1, "Completed", summary);
            }

            // ServiceTask: simulate external service call
            var step2 = "post-finalize.service.apply-policies";
            if (!IsStepLogged(workflowId, step2))
            {
                AppendStepLog(workflowId, step2, "Started");
                await Task.Delay(200, cancellationToken);
                AppendStepLog(workflowId, step2, "Completed", new { applied = true });
            }

            // Timer: wait boundary
            var step3 = "post-finalize.timer.wait";
            if (!IsStepLogged(workflowId, step3))
            {
                AppendStepLog(workflowId, step3, "Started", new { seconds = 3 });
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                AppendStepLog(workflowId, step3, "Fired");
            }

            // Event: emit completion
            var step4 = "post-finalize.event.completed";
            if (!IsStepLogged(workflowId, step4))
            {
                AppendStepLog(workflowId, step4, "Emitted");
            }

            state.Status = "Completed";
            state.Result = new { ok = true };
            AppendStepLog(workflowId, "workflow.completed", "Completed");
            _logger.LogInformation("Post-finalize workflow {WorkflowId} completed", workflowId);
        }
        catch (Exception ex)
        {
            AppendStepLog(workflowId, "post-finalize.compensation", "Triggered", new { error = ex.Message });
            state.Status = "Failed";
            state.Error = ex.Message;
            _logger.LogError(ex, "Error in post-finalize workflow {WorkflowId}", workflowId);
        }
    }

    private string GetInstanceDir(Guid workflowId) => Path.Combine(_storeRoot, workflowId.ToString());
    private string GetLogPath(Guid workflowId) => Path.Combine(GetInstanceDir(workflowId), "logs.ndjson");
    private void EnsureInstanceDir(Guid workflowId) => Directory.CreateDirectory(GetInstanceDir(workflowId));

    private bool IsStepLogged(Guid workflowId, string stepId)
    {
        if (_loggedSteps.TryGetValue(workflowId, out var set) && set.Contains(stepId))
            return true;

        var path = GetLogPath(workflowId);
        if (File.Exists(path))
        {
            foreach (var line in File.ReadLines(path))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("stepId", out var sid) && sid.GetString() == stepId)
                    {
                        if (!_loggedSteps.ContainsKey(workflowId)) _loggedSteps[workflowId] = new HashSet<string>();
                        _loggedSteps[workflowId].Add(stepId);
                        return true;
                    }
                }
                catch { /* skip malformed */ }
            }
        }
        return false;
    }

    private void AppendStepLog(Guid workflowId, string stepId, string status, object? data = null)
    {
        EnsureInstanceDir(workflowId);
        var path = GetLogPath(workflowId);
        var entry = new { instanceId = workflowId, stepId, status, timestamp = DateTime.UtcNow, data };
        var line = JsonSerializer.Serialize(entry);
        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        if (!_loggedSteps.ContainsKey(workflowId)) _loggedSteps[workflowId] = new HashSet<string>();
        _loggedSteps[workflowId].Add(stepId);
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