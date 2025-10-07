using CoreAxis.Modules.WalletModule.Domain.Entities;

namespace CoreAxis.Modules.WalletModule.Application.Services;

public interface ITransactionService
{
    Task<Transaction> ExecuteDepositAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<Transaction> ExecuteWithdrawalAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<(Transaction DebitTransaction, Transaction CreditTransaction)> ExecuteTransferAsync(
        Guid fromWalletId,
        Guid toWalletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Transaction> ExecuteCommissionCreditAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}