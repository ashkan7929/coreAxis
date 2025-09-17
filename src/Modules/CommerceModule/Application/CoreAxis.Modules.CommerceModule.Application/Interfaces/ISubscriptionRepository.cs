using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id);
    Task<Subscription?> GetByIdWithPaymentsAsync(Guid id);
    Task<(List<Subscription> Subscriptions, int TotalCount)> GetSubscriptionsAsync(
        Guid? userId = null,
        string? status = null,
        string? planName = null,
        bool? autoRenew = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<List<Subscription>> GetActiveSubscriptionsByUserIdAsync(Guid userId);
    Task<List<Subscription>> GetSubscriptionsDueForRenewalAsync(DateTime date);
    Task<Subscription> AddAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> HasActiveSubscriptionAsync(Guid userId, string planName);
}

public interface ISubscriptionPaymentRepository
{
    Task<SubscriptionPayment?> GetByIdAsync(Guid id);
    Task<SubscriptionPayment?> GetByTransactionIdAsync(string transactionId);
    Task<List<SubscriptionPayment>> GetPaymentsBySubscriptionIdAsync(Guid subscriptionId);
    Task<(List<SubscriptionPayment> Payments, int TotalCount)> GetSubscriptionPaymentsAsync(
        Guid? subscriptionId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<SubscriptionPayment> AddAsync(SubscriptionPayment payment);
    Task<SubscriptionPayment> UpdateAsync(SubscriptionPayment payment);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<SubscriptionPayment?> GetLastSuccessfulPaymentAsync(Guid subscriptionId);
    Task<List<SubscriptionPayment>> GetFailedPaymentsAsync(Guid subscriptionId);
}