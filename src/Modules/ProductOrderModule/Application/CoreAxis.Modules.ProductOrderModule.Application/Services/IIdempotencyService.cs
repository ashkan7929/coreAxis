namespace CoreAxis.Modules.ProductOrderModule.Application.Services;

public interface IIdempotencyService
{
    /// <summary>
    /// Checks if a request with given key/operation/hash has been processed.
    /// </summary>
    Task<bool> IsRequestProcessedAsync(string idempotencyKey, string operation, string requestHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a request as processed with given key/operation/hash.
    /// </summary>
    Task MarkRequestProcessedAsync(string idempotencyKey, string operation, string requestHash, CancellationToken cancellationToken = default);
}