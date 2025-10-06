using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Application.Events;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;

public class ProductEventsOptions
{
    public bool Enabled { get; set; } = false;
    public string TenantId { get; set; } = "default";
}

public interface IProductEventEmitter
{
    Task EmitUpdatedAsync(Product product, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default);
    Task EmitStatusChangedAsync(Product product, string newStatus, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default);
}

public class ProductEventEmitter : IProductEventEmitter
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<ProductEventEmitter> _logger;
    private readonly ProductEventsOptions _options;

    public ProductEventEmitter(IOutboxService outboxService, ILogger<ProductEventEmitter> logger, IOptions<ProductEventsOptions> options)
    {
        _outboxService = outboxService;
        _logger = logger;
        _options = options.Value ?? new ProductEventsOptions();
    }

    public async Task EmitUpdatedAsync(Product product, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Product events disabled; skipping ProductUpdated for {ProductId}", product.Id);
            return;
        }

        var evt = new ProductUpdated(
            product.Id,
            product.Code,
            product.Name,
            product.PriceFrom?.Amount ?? 0m,
            product.PriceFrom?.Currency ?? "USD",
            product.Attributes,
            _options.TenantId,
            correlationId,
            causationId);

        var message = new OutboxMessage(
            evt.GetType().AssemblyQualifiedName!,
            JsonSerializer.Serialize(evt),
            evt.CorrelationId,
            evt.CausationId,
            evt.TenantId);
        await _outboxService.AddMessageAsync(message, cancellationToken);
        _logger.LogInformation("Enqueued ProductUpdated for {ProductId}", product.Id);
    }

    public async Task EmitStatusChangedAsync(Product product, string newStatus, Guid correlationId, Guid? causationId = null, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Product events disabled; skipping ProductStatusChanged for {ProductId}", product.Id);
            return;
        }

        var evt = new ProductStatusChanged(
            product.Id,
            product.Code,
            newStatus,
            _options.TenantId,
            correlationId,
            causationId);

        var message = new OutboxMessage(
            evt.GetType().AssemblyQualifiedName!,
            JsonSerializer.Serialize(evt),
            evt.CorrelationId,
            evt.CausationId,
            evt.TenantId);
        await _outboxService.AddMessageAsync(message, cancellationToken);
        _logger.LogInformation("Enqueued ProductStatusChanged for {ProductId} -> {Status}", product.Id, newStatus);
    }
}