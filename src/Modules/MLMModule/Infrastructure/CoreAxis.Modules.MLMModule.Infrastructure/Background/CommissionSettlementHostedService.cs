using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using CoreAxis.SharedKernel.Outbox;
using CoreAxis.SharedKernel.Contracts.Events;
using System.Text.Json;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Background;

public class CommissionSettlementHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommissionSettlementHostedService> _logger;

    private static readonly Meter _meter = new("CoreAxis.MLM");
    private static readonly Counter<int> _bookedCounter = _meter.CreateCounter<int>("mlm.commissions.booked.count");
    private static readonly Counter<int> _failuresCounter = _meter.CreateCounter<int>("mlm.failures.count");
    private static readonly Histogram<double> _latencyMs = _meter.CreateHistogram<double>("mlm.commissions.settlement.latency", unit: "ms");
    private static readonly Counter<int> _bookedByLevelCounter = _meter.CreateCounter<int>("mlm.commissions.booked.by_level");
    private static readonly Counter<int> _failuresByCodeCounter = _meter.CreateCounter<int>("mlm.failures.by_code");
    private static volatile int _pendingSnapshot = 0;
    private static readonly ObservableGauge<int> _pendingGauge = _meter.CreateObservableGauge<int>(
        "mlm.commissions.pending",
        () => new Measurement<int>[] { new(_pendingSnapshot) }
    );

    private readonly TimeSpan _interval;
    private readonly int _batchSize;

    public CommissionSettlementHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CommissionSettlementHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Defaults; can be overridden via configuration in DI registration
        _interval = TimeSpan.FromMinutes(2);
        _batchSize = 50;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MLM CommissionSettlementHostedService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStart = DateTime.UtcNow;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var commissionRepo = scope.ServiceProvider.GetRequiredService<ICommissionTransactionRepository>();
                var walletRepo = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
                var walletTypeRepo = scope.ServiceProvider.GetRequiredService<IWalletTypeRepository>();
                var txnService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                // Fetch approved commissions that are not yet settled and without a wallet txn
                var approved = await commissionRepo.GetByStatusAsync(CommissionStatus.Approved, 0, _batchSize, stoppingToken);

                var pendingForBooking = approved
                    .Where(c => !c.IsSettled && c.WalletTransactionId == null)
                    .ToList();

                // Update pending commissions gauge snapshot
                _pendingSnapshot = pendingForBooking.Count;

                if (pendingForBooking.Count == 0)
                {
                    _logger.LogDebug("No approved commissions pending booking in this cycle");
                }

                foreach (var commission in pendingForBooking)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        // Ensure commission wallet exists
                        var commissionWalletType = await walletTypeRepo.GetByNameAsync("Commission", stoppingToken)
                            ?? throw new InvalidOperationException("Wallet type 'Commission' not found");

                        var wallet = await walletRepo.GetByUserAndTypeAsync(commission.UserId, commissionWalletType.Id, stoppingToken);
                        if (wallet == null)
                        {
                            wallet = new Wallet(commission.UserId, commissionWalletType.Id);
                            await walletRepo.AddAsync(wallet, stoppingToken);
                            await walletRepo.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Created commission wallet for user {UserId}", commission.UserId);
                        }

                        // Build idempotency key per spec
                        var idempotencyKey = $"commission:{commission.SourcePaymentId}:{commission.UserId}:L{commission.Level}";
                        var description = $"Commission level {commission.Level} for payment {commission.SourcePaymentId}";
                        var reference = commission.SourcePaymentId.ToString();
                        var metadata = new
                        {
                            CommissionId = commission.Id,
                            commission.UserId,
                            commission.Level,
                            commission.RuleSetCode,
                            commission.RuleVersion,
                            commission.SourceAmount,
                            commission.Percentage
                        };

                        // Create a commission credit transaction in Wallet (booking step)
                        var txn = await txnService.ExecuteCommissionCreditAsync(
                            wallet.Id,
                            commission.Amount,
                            description,
                            reference,
                            metadata,
                            idempotencyKey,
                            commission.CorrelationId,
                            stoppingToken);

                        // Persist WalletTransactionId into commission; do NOT mark as Paid here
                        // Paid will be set by wallet settlement completing the credit
                        typeof(CommissionTransaction)
                            .GetProperty(nameof(CommissionTransaction.WalletTransactionId))!
                            .SetValue(commission, txn.Id);

                        await commissionRepo.UpdateAsync(commission, stoppingToken);

                        // Publish CommissionBooked integration event via Outbox
                        var correlationGuid = Guid.TryParse(commission.CorrelationId, out var cid) ? cid : Guid.NewGuid();
                        var commissionBookedEvent = new CommissionBooked(
                            commissionId: commission.Id,
                            userId: commission.UserId,
                            walletTransactionId: txn.Id,
                            amount: commission.Amount,
                            level: commission.Level,
                            ruleSetCode: commission.RuleSetCode,
                            ruleVersion: commission.RuleVersion,
                            sourcePaymentId: commission.SourcePaymentId,
                            productId: commission.ProductId,
                            bookedAt: DateTime.UtcNow,
                            tenantId: "default",
                            correlationId: correlationGuid,
                            causationId: commission.Id
                        );

                        var eventJson = JsonSerializer.Serialize(commissionBookedEvent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        var outboxMessage = new OutboxMessage(
                            type: "CommissionBooked.v1",
                            content: eventJson,
                            correlationId: correlationGuid,
                            causationId: commission.Id,
                            tenantId: "default",
                            maxRetries: 3
                        );

                        await outboxRepository.AddAsync(outboxMessage, stoppingToken);

                        _bookedCounter.Add(1);
                        _bookedByLevelCounter.Add(1, new KeyValuePair<string, object?>("level", commission.Level));
                        _latencyMs.Record(sw.Elapsed.TotalMilliseconds);

                        _logger.LogInformation(
                            "Booked commission {CommissionId} for user {UserId} amount {Amount} with wallet txn {WalletTxnId}",
                            commission.Id, commission.UserId, commission.Amount, txn.Id);
                    }
                    catch (Exception ex)
                    {
                        _failuresCounter.Add(1);
                        _failuresByCodeCounter.Add(1, new KeyValuePair<string, object?>("code", "BOOKING_FAILED"));
                        _logger.LogError(ex, "Failed booking commission {CommissionId} for user {UserId}", commission.Id, commission.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Commission settlement cycle failed");
            }
            finally
            {
                var delay = _interval - (DateTime.UtcNow - cycleStart);
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(5);
                try { await Task.Delay(delay, stoppingToken); } catch { /* ignore */ }
            }
        }
    }
}