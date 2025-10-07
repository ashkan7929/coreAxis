using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Services;

public class CommissionSettlementHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommissionSettlementHostedService> _logger;

    private static readonly Meter _meter = new("CoreAxis.Wallet");
    private static readonly Counter<int> _settledCounter = _meter.CreateCounter<int>("wallet.commissions.settled.count");
    private static readonly Counter<int> _failuresCounter = _meter.CreateCounter<int>("wallet.failures.count");
    private static readonly Histogram<double> _latencyMs = _meter.CreateHistogram<double>("wallet.commissions.settlement.latency", unit: "ms");

    public CommissionSettlementHostedService(IServiceScopeFactory scopeFactory, ILogger<CommissionSettlementHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Commission settlement service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
                var transactionTypeRepo = scope.ServiceProvider.GetRequiredService<ITransactionTypeRepository>();

                // Get commission transaction type
                var commissionType = await transactionTypeRepo.GetByCodeAsync("COMMISSION", stoppingToken);
                if (commissionType == null)
                {
                    _logger.LogWarning("COMMISSION transaction type not configured; skipping settlement run.");
                }
                else
                {
                    // Fetch a batch of pending commission transactions
                    var pendingCommissions = await context.Transactions
                        .Include(t => t.Wallet)
                        .Where(t => t.TransactionTypeId == commissionType.Id && t.Status == TransactionStatus.Pending)
                        .OrderBy(t => t.CreatedOn)
                        .Take(200)
                        .ToListAsync(stoppingToken);

                    var settledCount = 0;

                    foreach (var txn in pendingCommissions)
                    {
                        try
                        {
                            var wallet = txn.Wallet;

                            // Skip locked wallets, record a failure metric for visibility
                            if (wallet.IsLocked)
                            {
                                _logger.LogWarning("SECURITY: Skipping commission settlement; wallet locked {WalletId}. UserId {UserId}. Reason {LockReason}",
                                    wallet.Id, wallet.UserId, wallet.LockReason);
                                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_ACCOUNT_FROZEN"));
                                continue;
                            }

                            // Credit wallet with commission amount and mark transaction completed
                            wallet.Credit(txn.Amount, $"Commission settlement for transaction {txn.Id}");

                            // Update transaction bookkeeping
                            // BalanceAfter reflects wallet balance after the credit
                            typeof(Transaction)
                                .GetProperty("BalanceAfter")!
                                .SetValue(txn, wallet.Balance);

                            // Mark transaction as completed
                            txn.Complete();

                            // Set processed timestamp to now for settlement
                            typeof(Transaction)
                                .GetProperty("ProcessedAt")!
                                .SetValue(txn, DateTime.UtcNow);

                            // Stage changes in context
                            context.Wallets.Update(wallet);
                            context.Transactions.Update(txn);

                            settledCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error settling commission transaction {TransactionId}", txn.Id);
                            _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_SETTLEMENT_ERR"));
                        }
                    }

                    if (settledCount > 0)
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }

                    _settledCounter.Add(settledCount);
                    _logger.LogInformation("Commission settlement run completed. Settled {SettledCount} transactions.", settledCount);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Commission settlement run failed.");
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_SERVICE_ERR"));
            }
            finally
            {
                sw.Stop();
                _latencyMs.Record(sw.Elapsed.TotalMilliseconds);
            }

            // Delay between runs
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Commission settlement service stopped.");
    }
}