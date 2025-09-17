using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Subscriptions;

public record GetSubscriptionByIdQuery(Guid Id) : IRequest<SubscriptionDto?>;

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, SubscriptionDto?>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<GetSubscriptionByIdQueryHandler> _logger;

    public GetSubscriptionByIdQueryHandler(
        ISubscriptionRepository subscriptionRepository,
        ILogger<GetSubscriptionByIdQueryHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<SubscriptionDto?> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdWithPaymentsAsync(request.Id);
            if (subscription == null)
            {
                _logger.LogWarning("Subscription with ID {SubscriptionId} not found", request.Id);
                return null;
            }

            var subscriptionDto = new SubscriptionDto
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
                Payments = subscription.Payments?.Select(payment => new SubscriptionPaymentDto
                {
                    Id = payment.Id,
                    SubscriptionId = payment.SubscriptionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    TransactionId = payment.TransactionId,
                    BillingPeriodStart = payment.BillingPeriodStart,
                    BillingPeriodEnd = payment.BillingPeriodEnd,
                    ProcessedAt = payment.ProcessedAt,
                    FailureReason = payment.FailureReason,
                    CreatedAt = payment.CreatedAt,
                    UpdatedAt = payment.UpdatedAt
                }).ToList() ?? new List<SubscriptionPaymentDto>()
            };

            _logger.LogInformation("Retrieved subscription with ID: {SubscriptionId}", request.Id);
            return subscriptionDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription with ID: {SubscriptionId}", request.Id);
            throw;
        }
    }
}