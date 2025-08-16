using CoreAxis.Modules.WalletModule.Domain.Events;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoreAxis.Modules.WalletModule.Infrastructure.EventHandlers;

public class TransactionCreatedEventHandler : IDomainEventHandler<TransactionCreatedEvent>
{
    private readonly WalletDbContext _context;
    private readonly ILogger<TransactionCreatedEventHandler> _logger;

    public TransactionCreatedEventHandler(
        WalletDbContext context,
        ILogger<TransactionCreatedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(TransactionCreatedEvent domainEvent)
    {
        try
        {
            _logger.LogDebug("Handling TransactionCreatedEvent for transaction: {TransactionId}", 
                domainEvent.TransactionId);

            // Get the transaction to access CorrelationId
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == domainEvent.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", domainEvent.TransactionId);
                return;
            }

            // Create integration event contract
            var eventContract = new
            {
                TransactionId = domainEvent.TransactionId,
                WalletId = domainEvent.WalletId,
                Amount = domainEvent.Amount,
                TransactionTypeId = domainEvent.TransactionTypeId,
                OccurredOn = domainEvent.OccurredOn
            };

            var eventJson = JsonSerializer.Serialize(eventContract);
            var eventTypeName = "TransactionCreated.v1";

            var outboxMessage = new OutboxMessage(
                type: eventTypeName,
                content: eventJson,
                correlationId: transaction.CorrelationId ?? Guid.NewGuid(),
                causationId: null, // Domain events don't have causation
                tenantId: "default", // TODO: Get from context
                maxRetries: 3
            );

            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Successfully persisted TransactionCreatedEvent to outbox: {TransactionId}", 
                domainEvent.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist TransactionCreatedEvent to outbox: {TransactionId}", 
                domainEvent.TransactionId);
            throw;
        }
    }
}