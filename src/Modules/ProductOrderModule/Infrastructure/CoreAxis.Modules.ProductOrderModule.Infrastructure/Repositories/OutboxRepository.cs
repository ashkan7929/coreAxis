using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly ProductOrderDbContext _context;

    public OutboxRepository(ProductOrderDbContext context)
    {
        _context = context;
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedOn == null && 
                       (m.NextRetryAt == null || m.NextRetryAt <= DateTime.UtcNow) &&
                       m.RetryCount < m.MaxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
}