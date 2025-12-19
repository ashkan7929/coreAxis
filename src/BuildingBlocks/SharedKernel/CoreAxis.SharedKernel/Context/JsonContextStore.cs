using System.Collections.Concurrent;

namespace CoreAxis.SharedKernel.Context;

public class JsonContextStore : IContextStore
{
    // Simple in-memory implementation for now, can be replaced with DB/Redis backed one
    private readonly ConcurrentDictionary<string, ContextDocument> _store = new();

    public Task<ContextDocument> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var context))
        {
            return Task.FromResult(context);
        }
        return Task.FromResult(new ContextDocument());
    }

    public Task SaveAsync(string key, ContextDocument context, CancellationToken cancellationToken = default)
    {
        _store.AddOrUpdate(key, context, (_, _) => context);
        return Task.CompletedTask;
    }
}
