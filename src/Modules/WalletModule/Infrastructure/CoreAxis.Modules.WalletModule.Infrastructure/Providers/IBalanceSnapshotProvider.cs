using System.Collections.Concurrent;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Providers;

public interface IBalanceSnapshotProvider
{
    int SnapshotCount { get; }
    Task SetSnapshotAsync(Guid walletId, decimal balance, DateTime capturedAt, CancellationToken cancellationToken = default);
    Task<BalanceSnapshot?> GetSnapshotAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BalanceSnapshot>> GetSnapshotsAsync(IEnumerable<Guid> walletIds, CancellationToken cancellationToken = default);
}