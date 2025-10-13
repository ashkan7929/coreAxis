namespace CoreAxis.Modules.Workflow.Application.Idempotency;

public interface IIdempotencyService
{
    Task<(bool found, int statusCode, string? responseJson)> TryGetAsync(string route, string key, string bodyHash, CancellationToken ct = default);
    Task StoreAsync(string route, string key, string bodyHash, int statusCode, string? responseJson, CancellationToken ct = default);
}