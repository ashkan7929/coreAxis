using CoreAxis.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.SharedKernel.Outbox;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var unprocessedMessages = await outboxRepository.GetUnprocessedMessagesAsync(50, cancellationToken);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                // Deserialize the event
                var eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    _logger.LogWarning("Unknown event type: {EventType}", message.Type);
                    message.MarkAsFailed($"Unknown event type: {message.Type}");
                    await outboxRepository.UpdateAsync(message, cancellationToken);
                    continue;
                }

                var eventData = JsonSerializer.Deserialize(message.Content, eventType);
                if (eventData == null)
                {
                    _logger.LogWarning("Failed to deserialize event: {MessageId}", message.Id);
                    message.MarkAsFailed("Failed to deserialize event");
                    await outboxRepository.UpdateAsync(message, cancellationToken);
                    continue;
                }

                // Publish the event
                await eventBus.PublishDynamicAsync(eventData, eventType.Name);

                // Mark as processed
                message.MarkAsProcessed();
                await outboxRepository.UpdateAsync(message, cancellationToken);

                _logger.LogDebug("Successfully published outbox message: {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message: {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);
                await outboxRepository.UpdateAsync(message, cancellationToken);
            }
        }
    }
}

public interface IOutboxRepository
{
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}