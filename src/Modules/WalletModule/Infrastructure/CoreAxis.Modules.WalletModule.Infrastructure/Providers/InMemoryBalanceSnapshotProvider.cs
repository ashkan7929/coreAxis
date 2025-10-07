using System.Collections.Concurrent;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Providers;

public class InMemoryBalanceSnapshotProvider : IBalanceSnapshotProvider
{
    private readonly ConcurrentDictionary<Guid, BalanceSnapshot> _snapshots = new();

    public int SnapshotCount => _snapshots.Count;

    public Task SetSnapshotAsync(Guid walletId, decimal balance, DateTime capturedAt, CancellationToken cancellationToken = default)
    {
        _snapshots[walletId] = new BalanceSnapshot
        {
            WalletId = walletId,
            Balance = balance,
            CapturedAt = capturedAt
        };
        return Task.CompletedTask;
    }

    public Task<BalanceSnapshot?> GetSnapshotAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        _snapshots.TryGetValue(walletId, out var snapshot);
        return Task.FromResult<BalanceSnapshot?>(snapshot);
    }

    public Task<IReadOnlyCollection<BalanceSnapshot>> GetSnapshotsAsync(IEnumerable<Guid> walletIds, CancellationToken cancellationToken = default)
    {
        var list = new List<BalanceSnapshot>();
        foreach (var id in walletIds)
        {
            if (_snapshots.TryGetValue(id, out var snap))
            {
                list.Add(snap);
            }
        }
        return Task.FromResult<IReadOnlyCollection<BalanceSnapshot>>(list);
    }
}