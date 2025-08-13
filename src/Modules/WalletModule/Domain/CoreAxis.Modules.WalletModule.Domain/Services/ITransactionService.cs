using CoreAxis.Modules.WalletModule.Domain.Entities;

namespace CoreAxis.Modules.WalletModule.Domain.Services;

public interface ITransactionService
{
    /// <summary>
    /// Executes a deposit transaction atomically with idempotency support
    /// </summary>
    Task<Transaction> ExecuteDepositAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a withdrawal transaction atomically with idempotency support
    /// </summary>
    Task<Transaction> ExecuteWithdrawalAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a transfer transaction atomically with idempotency support
    /// </summary>
    Task<(Transaction OutTransaction, Transaction InTransaction)> ExecuteTransferAsync(
        Guid fromWalletId,
        Guid toWalletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a transaction with the given idempotency key already exists
    /// </summary>
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}