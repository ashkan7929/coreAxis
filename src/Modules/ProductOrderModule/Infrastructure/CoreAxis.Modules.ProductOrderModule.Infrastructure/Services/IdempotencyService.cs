using CoreAxis.Modules.ProductOrderModule.Application.Services;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly ProductOrderDbContext _dbContext;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(ProductOrderDbContext dbContext, ILogger<IdempotencyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> IsRequestProcessedAsync(string idempotencyKey, string operation, string requestHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return false;

        var existsByKey = await _dbContext.IdempotencyEntries
            .AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existsByKey)
        {
            _logger.LogDebug("Idempotency hit by key {Key} for {Operation}", idempotencyKey, operation);
            return true;
        }

        var existsByHash = await _dbContext.IdempotencyEntries
            .AsNoTracking()
            .AnyAsync(x => x.Operation == operation && x.RequestHash == requestHash, cancellationToken);

        if (existsByHash)
        {
            _logger.LogDebug("Idempotency hit by hash for {Operation}", operation);
            return true;
        }

        return false;
    }

    public async Task MarkRequestProcessedAsync(string idempotencyKey, string operation, string requestHash, CancellationToken cancellationToken = default)
    {
        var entry = new IdempotencyEntry
        {
            IdempotencyKey = idempotencyKey,
            Operation = operation,
            RequestHash = requestHash
        };

        await _dbContext.IdempotencyEntries.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Idempotency recorded for {Operation} with key {Key}", operation, idempotencyKey);
    }
}