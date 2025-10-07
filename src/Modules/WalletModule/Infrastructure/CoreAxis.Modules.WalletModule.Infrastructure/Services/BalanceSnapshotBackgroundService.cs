using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.Modules.WalletModule.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Services;

public class BalanceSnapshotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BalanceSnapshotBackgroundService> _logger;

    public BalanceSnapshotBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BalanceSnapshotBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Balance snapshot service started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
                var provider = scope.ServiceProvider.GetRequiredService<IBalanceSnapshotProvider>();

                var wallets = await context.Wallets
                    .OrderBy(w => w.Id)
                    .Take(1000) // avoid heavy full scans
                    .Select(w => new { w.Id, w.Balance })
                    .ToListAsync(stoppingToken);

                var now = DateTime.UtcNow;
                foreach (var w in wallets)
                {
                    await provider.SetSnapshotAsync(w.Id, w.Balance, now, stoppingToken);
                }

                _logger.LogInformation("Captured {Count} wallet balance snapshots.", wallets.Count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing wallet balance snapshots.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        _logger.LogInformation("Balance snapshot service stopped.");
    }
}