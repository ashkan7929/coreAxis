using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Subscriptions;

public record CancelSubscriptionCommand(
    Guid SubscriptionId,
    string CancellationReason,
    bool ImmediateCancellation = false
) : IRequest<SubscriptionDto>;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, SubscriptionDto>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<SubscriptionDto> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(request.SubscriptionId);
            if (subscription == null)
            {
                throw new InvalidOperationException($"Subscription with ID {request.SubscriptionId} not found.");
            }

            if (subscription.Status == "Cancelled")
            {
                throw new InvalidOperationException($"Subscription with ID {request.SubscriptionId} is already cancelled.");
            }

            subscription.Status = "Cancelled";
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancellationReason = request.CancellationReason;
            subscription.AutoRenew = false;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Set end date based on cancellation type
            if (request.ImmediateCancellation)
            {
                subscription.EndDate = DateTime.UtcNow;
            }
            else
            {
                // Cancel at the end of current billing period
                subscription.EndDate = subscription.NextBillingDate;
            }

            await _subscriptionRepository.UpdateAsync(subscription);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation("Subscription cancelled successfully with ID: {SubscriptionId}. Reason: {Reason}", 
                subscription.Id, request.CancellationReason);

            return new SubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                PlanName = subscription.PlanName,
                Status = subscription.Status,
                Price = subscription.Price,
                Currency = subscription.Currency,
                BillingCycle = subscription.BillingCycle,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                NextBillingDate = subscription.NextBillingDate,
                CancelledAt = subscription.CancelledAt,
                CancellationReason = subscription.CancellationReason,
                AutoRenew = subscription.AutoRenew,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt,
                Payments = new List<SubscriptionPaymentDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription: {SubscriptionId}", request.SubscriptionId);
            throw;
        }
    }
}