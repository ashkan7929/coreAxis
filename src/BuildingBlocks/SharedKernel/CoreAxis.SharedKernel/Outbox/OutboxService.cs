using Microsoft.Extensions.Logging;

namespace CoreAxis.SharedKernel.Outbox;

public class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IOutboxRepository outboxRepository, ILogger<OutboxService> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _outboxRepository.AddAsync(message, cancellationToken);
            _logger.LogDebug("Added outbox message {MessageId} of type {MessageType}", message.Id, message.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add outbox message {MessageId} of type {MessageType}", message.Id, message.Type);
            throw;
        }
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _outboxRepository.GetUnprocessedMessagesAsync(batchSize, cancellationToken);
            _logger.LogDebug("Retrieved {Count} unprocessed outbox messages", messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve unprocessed outbox messages");
            throw;
        }
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _outboxRepository.GetByIdAsync(messageId, cancellationToken);
            if (message != null)
            {
                message.MarkAsProcessed();
                await _outboxRepository.UpdateAsync(message, cancellationToken);
                _logger.LogDebug("Marked outbox message {MessageId} as processed", messageId);
            }
            else
            {
                _logger.LogWarning("Outbox message {MessageId} not found when trying to mark as processed", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark outbox message {MessageId} as processed", messageId);
            throw;
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _outboxRepository.GetByIdAsync(messageId, cancellationToken);
            if (message != null)
            {
                message.MarkAsFailed(error);
                await _outboxRepository.UpdateAsync(message, cancellationToken);
                _logger.LogDebug("Marked outbox message {MessageId} as failed with error: {Error}", messageId, error);
            }
            else
            {
                _logger.LogWarning("Outbox message {MessageId} not found when trying to mark as failed", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark outbox message {MessageId} as failed", messageId);
            throw;
        }
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _outboxRepository.GetByIdAsync(id, cancellationToken);
            _logger.LogDebug("Retrieved outbox message {MessageId}", id);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve outbox message {MessageId}", id);
            throw;
        }
    }
}