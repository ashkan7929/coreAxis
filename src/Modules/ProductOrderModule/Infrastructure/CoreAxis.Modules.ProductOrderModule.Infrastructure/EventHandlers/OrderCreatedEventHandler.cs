using CoreAxis.Modules.ProductOrderModule.Domain.Events;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;

public class OrderCreatedEventHandler : IDomainEventHandler<OrderCreatedEvent>
{
    private readonly ProductOrderDbContext _context;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        ProductOrderDbContext context,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedEvent domainEvent)
    {
        try
        {
            _logger.LogDebug("Handling OrderCreatedEvent for Order: {OrderId}", domainEvent.OrderId);

            // Create OrderPlaced integration event
            var orderPlacedEvent = new OrderPlaced(
                orderId: domainEvent.OrderId,
                userId: domainEvent.UserId,
                assetCode: domainEvent.AssetCode,
                quantity: domainEvent.Quantity,
                tenantId: domainEvent.TenantId,
                metadata: new Dictionary<string, object>
                {
                    { "source", "ProductOrderModule" },
                    { "version", "v1" },
                    { "timestamp", DateTime.UtcNow }
                },
                correlationId: Guid.NewGuid(),
                causationId: domainEvent.Id
            );

            // Serialize the event
            var eventJson = JsonSerializer.Serialize(orderPlacedEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Create outbox message
            var outboxMessage = new OutboxMessage(
                type: "OrderPlaced.v1",
                content: eventJson,
                correlationId: orderPlacedEvent.CorrelationId,
                causationId: orderPlacedEvent.CausationId,
                tenantId: domainEvent.TenantId,
                maxRetries: 3
            );

            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Successfully persisted OrderPlaced.v1 event to outbox: {OrderId}", 
                domainEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist OrderPlaced.v1 event to outbox: {OrderId}", 
                domainEvent.OrderId);
            throw;
        }
    }
}