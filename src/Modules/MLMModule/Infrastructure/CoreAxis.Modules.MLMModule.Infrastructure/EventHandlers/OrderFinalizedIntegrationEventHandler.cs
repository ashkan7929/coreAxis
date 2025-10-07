using System;
using System.Threading.Tasks;
using CoreAxis.EventBus;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles OrderFinalized integration events by generating Pending commission transactions.
/// Uses the default commission rule set when a product binding is not available.
/// </summary>
public class OrderFinalizedIntegrationEventHandler : IIntegrationEventHandler<OrderFinalized>
{
    private readonly ICommissionCalculationService _commissionCalculationService;
    private readonly ILogger<OrderFinalizedIntegrationEventHandler> _logger;

    public OrderFinalizedIntegrationEventHandler(
        ICommissionCalculationService commissionCalculationService,
        ILogger<OrderFinalizedIntegrationEventHandler> logger)
    {
        _commissionCalculationService = commissionCalculationService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderFinalized @event)
    {
        try
        {
            _logger.LogInformation(
                "Processing OrderFinalized event for Order {OrderId}, User {UserId}, TotalAmount {TotalAmount}",
                @event.OrderId, @event.UserId, @event.TotalAmount);

            // We don't have a productId in OrderFinalized; use Guid.Empty to trigger default rule set fallback.
            var productId = Guid.Empty;
            var correlationId = @event.CorrelationId.ToString();

            var result = await _commissionCalculationService.ProcessPaymentConfirmedAsync(
                sourcePaymentId: @event.OrderId,
                productId: productId,
                amount: @event.TotalAmount,
                buyerUserId: @event.UserId,
                correlationId: correlationId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "OrderFinalized commission processing failed for Order {OrderId}: {Error}",
                    @event.OrderId, string.Join("; ", result.Errors));
                return;
            }

            _logger.LogInformation(
                "Generated {Count} Pending commission transactions for Order {OrderId}",
                result.Value.Count, @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderFinalized event for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}