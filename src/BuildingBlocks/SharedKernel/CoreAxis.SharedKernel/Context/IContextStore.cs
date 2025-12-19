namespace CoreAxis.SharedKernel.Context;

public interface IContextStore
{
    Task<ContextDocument> LoadAsync(string key, CancellationToken cancellationToken = default);
    Task SaveAsync(string key, ContextDocument context, CancellationToken cancellationToken = default);
}
