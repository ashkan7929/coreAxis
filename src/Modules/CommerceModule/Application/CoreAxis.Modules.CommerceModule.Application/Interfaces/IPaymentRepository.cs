using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByIdWithRefundsAsync(Guid id);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<(List<Payment> Payments, int TotalCount)> GetPaymentsAsync(
        Guid? orderId = null,
        string? status = null,
        string? paymentMethod = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<List<Payment>> GetPaymentsByOrderIdAsync(Guid orderId);
    Task<Payment> AddAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> TransactionIdExistsAsync(string transactionId, Guid? excludeId = null);
    Task<decimal> GetTotalAmountByOrderIdAsync(Guid orderId);
    Task<decimal> GetSuccessfulPaymentAmountByOrderIdAsync(Guid orderId);
}

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid id);
    Task<Refund?> GetByTransactionIdAsync(string refundTransactionId);
    Task<List<Refund>> GetRefundsByPaymentIdAsync(Guid paymentId);
    Task<(List<Refund> Refunds, int TotalCount)> GetRefundsAsync(
        Guid? paymentId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<Refund> AddAsync(Refund refund);
    Task<Refund> UpdateAsync(Refund refund);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<decimal> GetTotalRefundAmountByPaymentIdAsync(Guid paymentId);
}