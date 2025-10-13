using CoreAxis.Modules.Workflow.Application.Idempotency;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Application.Idempotency;

public class IdempotencyService : IIdempotencyService
{
    private readonly WorkflowDbContext _db;

    public IdempotencyService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<(bool found, int statusCode, string? responseJson)> TryGetAsync(string route, string key, string bodyHash, CancellationToken ct = default)
    {
        var entry = await _db.IdempotencyKeys.FirstOrDefaultAsync(i => i.Route == route && i.Key == key && i.BodyHash == bodyHash, ct);
        if (entry == null) return (false, 0, null);
        return (true, entry.StatusCode, entry.ResponseJson);
    }

    public async Task StoreAsync(string route, string key, string bodyHash, int statusCode, string? responseJson, CancellationToken ct = default)
    {
        var entity = new IdempotencyKey
        {
            Id = Guid.NewGuid(),
            Route = route,
            Key = key,
            BodyHash = bodyHash,
            StatusCode = statusCode,
            ResponseJson = responseJson,
            CreatedAt = DateTime.UtcNow
        };
        _db.IdempotencyKeys.Add(entity);
        await _db.SaveChangesAsync(ct);
    }
}