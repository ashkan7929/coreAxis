using System;
using System.Text.Json;
using System.Threading.Tasks;
using CoreAxis.Modules.WalletModule.Domain.Events;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Outbox;
using CoreAxis.SharedKernel.Domain;

namespace CoreAxis.Modules.WalletModule.Infrastructure.EventHandlers
{
    public class WalletBalanceChangedEventHandler : IDomainEventHandler<WalletBalanceChangedEvent>
    {
        private readonly WalletDbContext _dbContext;

        public WalletBalanceChangedEventHandler(WalletDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task HandleAsync(WalletBalanceChangedEvent domainEvent)
        {
            var payload = new
            {
                WalletId = domainEvent.WalletId,
                NewBalance = domainEvent.NewBalance,
                AmountChanged = domainEvent.AmountChanged,
                OperationType = domainEvent.OperationType,
                Reason = domainEvent.Reason,
                OccurredOn = DateTimeOffset.UtcNow
            };

            var outboxMessage = new OutboxMessage(
                type: "WalletBalanceChanged.v1",
                content: JsonSerializer.Serialize(payload),
                correlationId: Guid.NewGuid(),
                causationId: domainEvent.Id,
                tenantId: "wallet",
                maxRetries: 3
            );

            await _dbContext.OutboxMessages.AddAsync(outboxMessage);
            await _dbContext.SaveChangesAsync();
        }
    }
}