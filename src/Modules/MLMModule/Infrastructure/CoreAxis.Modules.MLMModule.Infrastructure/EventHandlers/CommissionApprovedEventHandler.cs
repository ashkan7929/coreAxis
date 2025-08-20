using CoreAxis.Modules.MLMModule.Domain.Events;
using CoreAxis.Modules.MLMModule.Infrastructure.IntegrationEvents;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.EventBus;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles CommissionApprovedEvent to publish wallet deposit event.
/// This handler processes commission approval and publishes event for wallet integration.
/// </summary>
public class CommissionApprovedEventHandler : IDomainEventHandler<CommissionApprovedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<CommissionApprovedEventHandler> _logger;

    public CommissionApprovedEventHandler(
        IEventBus eventBus,
        ILogger<CommissionApprovedEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CommissionApprovedEvent by publishing wallet deposit event.
    /// </summary>
    /// <param name="domainEvent">The CommissionApprovedEvent domain event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(CommissionApprovedEvent domainEvent)
    {
        try
        {
            _logger.LogInformation("Processing CommissionApprovedEvent for Commission {CommissionId}, User {UserId}, Amount {Amount}", 
                domainEvent.CommissionId, domainEvent.UserId, domainEvent.Amount);

            // Publish integration event for wallet module to handle the deposit
            var walletDepositEvent = new CommissionApprovedIntegrationEvent(
                commissionId: domainEvent.CommissionId,
                userId: domainEvent.UserId,
                amount: domainEvent.Amount,
                description: $"MLM Commission - Commission ID: {domainEvent.CommissionId}",
                reference: $"COMMISSION-{domainEvent.CommissionId}",
                idempotencyKey: $"commission-deposit-{domainEvent.CommissionId}"
            );

            await _eventBus.PublishAsync(walletDepositEvent);
            
            _logger.LogInformation("Commission approved event published for Commission {CommissionId}, User {UserId}, Amount {Amount}", 
                domainEvent.CommissionId, domainEvent.UserId, domainEvent.Amount);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CommissionApprovedEvent for Commission {CommissionId}, User {UserId}", 
                domainEvent.CommissionId, domainEvent.UserId);
            throw;
        }
    }
}