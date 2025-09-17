using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Subscriptions;

public record CreateSubscriptionCommand(
    Guid UserId,
    string PlanName,
    decimal Price,
    string Currency,
    string BillingCycle,
    DateTime StartDate,
    bool AutoRenew = true
) : IRequest<SubscriptionDto>;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, SubscriptionDto>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<SubscriptionDto> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Calculate next billing date based on billing cycle
            var nextBillingDate = CalculateNextBillingDate(request.StartDate, request.BillingCycle);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PlanName = request.PlanName,
                Status = "Active",
                Price = request.Price,
                Currency = request.Currency,
                BillingCycle = request.BillingCycle,
                StartDate = request.StartDate,
                NextBillingDate = nextBillingDate,
                AutoRenew = request.AutoRenew,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _subscriptionRepository.AddAsync(subscription);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation("Subscription created successfully with ID: {SubscriptionId} for User: {UserId}", subscription.Id, request.UserId);

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
            _logger.LogError(ex, "Error creating subscription for user: {UserId}", request.UserId);
            throw;
        }
    }

    private static DateTime CalculateNextBillingDate(DateTime startDate, string billingCycle)
    {
        return billingCycle.ToLower() switch
        {
            "monthly" => startDate.AddMonths(1),
            "quarterly" => startDate.AddMonths(3),
            "yearly" => startDate.AddYears(1),
            "weekly" => startDate.AddDays(7),
            _ => startDate.AddMonths(1) // Default to monthly
        };
    }
}