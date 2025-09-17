using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Subscriptions;

public record GetSubscriptionsQuery(
    Guid? UserId = null,
    string? Status = null,
    string? PlanName = null,
    bool? AutoRenew = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<(List<SubscriptionDto> Subscriptions, int TotalCount)>;

public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, (List<SubscriptionDto> Subscriptions, int TotalCount)>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<GetSubscriptionsQueryHandler> _logger;

    public GetSubscriptionsQueryHandler(
        ISubscriptionRepository subscriptionRepository,
        ILogger<GetSubscriptionsQueryHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<(List<SubscriptionDto> Subscriptions, int TotalCount)> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (subscriptions, totalCount) = await _subscriptionRepository.GetSubscriptionsAsync(
                request.UserId,
                request.Status,
                request.PlanName,
                request.AutoRenew,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize);

            var subscriptionDtos = subscriptions.Select(subscription => new SubscriptionDto
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
            }).ToList();

            _logger.LogInformation("Retrieved {Count} subscriptions out of {TotalCount} total subscriptions", 
                subscriptionDtos.Count, totalCount);

            return (subscriptionDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions");
            throw;
        }
    }
}