using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Interface for subscription service operations.
/// </summary>
public interface ISubscriptionService
{
    Task CreateInvoiceAsync(SubscriptionInvoice invoice, CancellationToken cancellationToken = default);
    Task RetryPaymentAsync(Guid invoiceId, string? correlationId = null, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(Guid subscriptionId, string reason, string? correlationId = null, CancellationToken cancellationToken = default);
}