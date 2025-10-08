using CoreAxis.EventBus;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics.Metrics;

namespace CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles PaymentConfirmed integration events to calculate and generate commission transactions.
/// This handler processes PaymentConfirmed.v1 events with idempotency protection.
/// </summary>
public class PaymentConfirmedEventHandler : IIntegrationEventHandler<PaymentConfirmed>
{
    private readonly ICommissionCalculationService _commissionCalculationService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly MLMModuleDbContext _context;
    private readonly ILogger<PaymentConfirmedEventHandler> _logger;

    private static readonly Meter _meter = new("CoreAxis.MLM");
    private static readonly Counter<int> _receivedCounter = _meter.CreateCounter<int>("mlm.events.payment_confirmed.received");
    private static readonly Counter<int> _skippedIdempotentCounter = _meter.CreateCounter<int>("mlm.events.payment_confirmed.skipped_idempotent");
    private static readonly Counter<int> _generatedCounter = _meter.CreateCounter<int>("mlm.commissions.generated.count");
    private static readonly Counter<int> _generationFailuresCounter = _meter.CreateCounter<int>("mlm.commissions.generation.failures");
    private static readonly Histogram<double> _generationLatencyMs = _meter.CreateHistogram<double>("mlm.commissions.generation.latency", unit: "ms");
    private static readonly Counter<int> _generatedByLevelCounter = _meter.CreateCounter<int>("mlm.commissions.generated.by_level");
    private static readonly Counter<int> _failuresByCodeCounter = _meter.CreateCounter<int>("mlm.failures.by_code");

    public PaymentConfirmedEventHandler(
        ICommissionCalculationService commissionCalculationService,
        IIdempotencyService idempotencyService,
        MLMModuleDbContext context,
        ILogger<PaymentConfirmedEventHandler> logger)
    {
        _commissionCalculationService = commissionCalculationService;
        _idempotencyService = idempotencyService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Handles the PaymentConfirmed integration event by calculating commissions and publishing CommissionGenerated events.
    /// </summary>
    /// <param name="event">The PaymentConfirmed integration event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(PaymentConfirmed @event)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _receivedCounter.Add(1);
            _logger.LogInformation("Processing PaymentConfirmed event for Payment {PaymentId}, Amount {Amount}", 
                @event.PaymentId, @event.Amount);

            // Check idempotency - if this payment has already been processed, skip
            var isAlreadyProcessed = await _idempotencyService.IsPaymentProcessedAsync(@event.PaymentId);
            if (isAlreadyProcessed)
            {
                _logger.LogInformation("Payment {PaymentId} has already been processed for commission calculation. Skipping.", 
                    @event.PaymentId);
                _skippedIdempotentCounter.Add(1);
                return;
            }

            // Start database transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Calculate commissions for this payment
                // Note: PaymentConfirmed event doesn't have ProductId or UserId, we need to get them from OrderId
                // For now, we'll use placeholder values until we implement proper order lookup
                var productId = Guid.Empty; // TODO: Get from order lookup
                var userId = Guid.Empty; // TODO: Get from order lookup
                
                var result = await _commissionCalculationService
                    .ProcessPaymentConfirmedAsync(@event.PaymentId, productId, @event.Amount, userId, @event.CorrelationId.ToString());
                
                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to process commission for payment {PaymentId}: {Errors}", @event.PaymentId, string.Join(", ", result.Errors));
                    _generationFailuresCounter.Add(1);
                    _failuresByCodeCounter.Add(1, new KeyValuePair<string, object?>("code", "CALCULATION_FAILED"));
                    return;
                }
                
                var commissionCalculations = result.Value;
                _generatedCounter.Add(commissionCalculations.Count);

                // Create outbox messages for each commission generated
                foreach (var calculation in commissionCalculations)
                {
                    _generatedByLevelCounter.Add(1, new KeyValuePair<string, object?>("level", calculation.Level));
                    var commissionGeneratedEvent = new CommissionGenerated(
                        sourcePaymentId: @event.PaymentId,
                        userId: calculation.UserId,
                        amount: calculation.Amount,
                        level: calculation.Level,
                        percentage: calculation.Percentage,
                        sourceAmount: calculation.SourceAmount,
                        userName: "Unknown", // TODO: Get from user service
                        userEmail: "unknown@example.com", // TODO: Get from user service
                        ruleSetName: "Default", // This should come from the calculation service
                        ruleSetVersion: 1, // This should come from the calculation service
                        tenantId: @event.TenantId,
                        correlationId: @event.CorrelationId,
                        causationId: @event.Id
                    );

                    var eventJson = JsonSerializer.Serialize(commissionGeneratedEvent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var outboxMessage = new OutboxMessage(
                        type: "CommissionGenerated.v1",
                        content: eventJson,
                        correlationId: @event.CorrelationId,
                        causationId: @event.Id,
                        tenantId: @event.TenantId,
                        maxRetries: 3
                    );

                    _context.OutboxMessages.Add(outboxMessage);
                }

                // Mark payment as processed (implicit through commission transaction creation)
                await _idempotencyService.MarkPaymentAsProcessedAsync(@event.PaymentId);

                // Save all changes in the same transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully processed PaymentConfirmed event for Payment {PaymentId}. Generated {CommissionCount} commission transactions.", 
                    @event.PaymentId, commissionCalculations.Count);
                _generationLatencyMs.Record(sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentConfirmed event for Payment {PaymentId}", @event.PaymentId);
            _generationFailuresCounter.Add(1);
            throw;
        }
    }
}