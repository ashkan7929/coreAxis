using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly WalletDbContext _context;
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        WalletDbContext context,
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<Transaction> ExecuteDepositAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Check for existing transaction with same idempotency key
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTransaction = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingTransaction != null)
            {
                _logger.LogInformation("Returning existing transaction for idempotency key: {IdempotencyKey}", idempotencyKey);
                return existingTransaction;
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get wallet with row-level locking
            var wallet = await _context.Wallets
                .Where(w => w.Id == walletId)
                .FirstOrDefaultAsync(cancellationToken);

            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet with ID {walletId} not found");
            }

            // Validate amount
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            // Get deposit transaction type
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("DEPOSIT", cancellationToken);
            if (transactionType == null)
            {
                throw new InvalidOperationException("DEPOSIT transaction type not configured");
            }

            // Credit wallet
            wallet.Credit(amount, description);

            // Create transaction record
            var newTransaction = new Transaction(
                wallet.Id,
                transactionType.Id,
                amount,
                wallet.Balance,
                description,
                reference,
                idempotencyKey,
                string.IsNullOrEmpty(correlationId) ? null : Guid.Parse(correlationId),
                metadata,
                null); // No related transaction for deposit

            newTransaction.Complete();

            // Save changes
            _context.Wallets.Update(wallet);
            await _context.Transactions.AddAsync(newTransaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Deposit of {Amount} completed for wallet {WalletId} with transaction {TransactionId}", 
                amount, walletId, newTransaction.Id);

            return newTransaction;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing deposit for wallet {WalletId}", walletId);
            throw;
        }
    }

    public async Task<Transaction> ExecuteWithdrawalAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Check for existing transaction with same idempotency key
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTransaction = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingTransaction != null)
            {
                _logger.LogInformation("Returning existing transaction for idempotency key: {IdempotencyKey}", idempotencyKey);
                return existingTransaction;
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get wallet with row-level locking
            var wallet = await _context.Wallets
                .Where(w => w.Id == walletId)
                .FirstOrDefaultAsync(cancellationToken);

            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet with ID {walletId} not found");
            }

            // Validate amount
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            // Check if wallet can be debited
            if (!wallet.CanDebit(amount))
            {
                var reason = wallet.IsLocked ? $"Wallet is locked: {wallet.LockReason}" : "Insufficient balance";
                throw new InvalidOperationException(reason);
            }

            // Get withdraw transaction type
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("WITHDRAW", cancellationToken);
            if (transactionType == null)
            {
                throw new InvalidOperationException("WITHDRAW transaction type not configured");
            }

            // Debit wallet
            wallet.Debit(amount, description);

            // Create transaction record
            var newTransaction = new Transaction(
                wallet.Id,
                transactionType.Id,
                -amount, // Negative for withdrawal
                wallet.Balance,
                description,
                reference,
                idempotencyKey,
                string.IsNullOrEmpty(correlationId) ? null : Guid.Parse(correlationId),
                metadata,
                null); // No related transaction for withdrawal

            newTransaction.Complete();

            // Save changes
            _context.Wallets.Update(wallet);
            await _context.Transactions.AddAsync(newTransaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Withdrawal of {Amount} completed for wallet {WalletId} with transaction {TransactionId}", 
                amount, walletId, newTransaction.Id);

            return newTransaction;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing withdrawal for wallet {WalletId}", walletId);
            throw;
        }
    }

    public async Task<(Transaction DebitTransaction, Transaction CreditTransaction)> ExecuteTransferAsync(
        Guid fromWalletId,
        Guid toWalletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Check for existing transaction with same idempotency key
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTransaction = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingTransaction != null)
            {
                _logger.LogInformation("Returning existing transaction for idempotency key: {IdempotencyKey}", idempotencyKey);
                
                // For transfers, we need to find the related transaction
                var relatedTransaction = await _transactionRepository.GetByIdAsync(existingTransaction.RelatedTransactionId!.Value, cancellationToken);
                if (relatedTransaction == null)
                {
                    throw new InvalidOperationException("Related transaction not found for existing transfer");
                }

                // Determine which is debit and which is credit based on amount sign
                if (existingTransaction.Amount < 0)
                {
                    return (existingTransaction, relatedTransaction);
                }
                else
                {
                    return (relatedTransaction, existingTransaction);
                }
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get both wallets with row-level locking (order by ID to prevent deadlocks)
            var walletIds = new[] { fromWalletId, toWalletId }.OrderBy(id => id).ToArray();
            var wallets = await _context.Wallets
                .Where(w => walletIds.Contains(w.Id))
                .OrderBy(w => w.Id)
                .ToListAsync(cancellationToken);

            var fromWallet = wallets.FirstOrDefault(w => w.Id == fromWalletId);
            var toWallet = wallets.FirstOrDefault(w => w.Id == toWalletId);

            if (fromWallet == null || toWallet == null)
            {
                throw new InvalidOperationException("One or both wallets not found");
            }

            // Validate amount
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            // Check if source wallet can be debited
            if (!fromWallet.CanDebit(amount))
            {
                var reason = fromWallet.IsLocked ? $"Source wallet is locked: {fromWallet.LockReason}" : "Insufficient balance";
                throw new InvalidOperationException(reason);
            }

            // Check if destination wallet is locked
            if (toWallet.IsLocked)
            {
                throw new InvalidOperationException($"Destination wallet is locked: {toWallet.LockReason}");
            }

            // Get transfer transaction types
            var transferOutType = await _transactionTypeRepository.GetByCodeAsync("TRANSFER_OUT", cancellationToken);
            var transferInType = await _transactionTypeRepository.GetByCodeAsync("TRANSFER_IN", cancellationToken);

            if (transferOutType == null || transferInType == null)
            {
                throw new InvalidOperationException("TRANSFER_OUT or TRANSFER_IN transaction types not configured");
            }

            // Perform transfer
            fromWallet.Debit(amount, description);
            toWallet.Credit(amount, description);

            // Create transaction records
            var outTransaction = new Transaction(
                fromWallet.Id,
                transferOutType.Id,
                -amount,
                fromWallet.Balance,
                description,
                reference,
                idempotencyKey,
                string.IsNullOrEmpty(correlationId) ? null : Guid.Parse(correlationId),
                metadata,
                null); // Will be set after creating inTransaction

            var inTransaction = new Transaction(
                toWallet.Id,
                transferInType.Id,
                amount,
                toWallet.Balance,
                description,
                reference,
                null, // Only the out transaction gets the idempotency key
                string.IsNullOrEmpty(correlationId) ? null : Guid.Parse(correlationId),
                metadata,
                outTransaction.Id);

            // Update the out transaction with the related transaction ID
            outTransaction = new Transaction(
                fromWallet.Id,
                transferOutType.Id,
                -amount,
                fromWallet.Balance,
                description,
                reference,
                idempotencyKey,
                string.IsNullOrEmpty(correlationId) ? null : Guid.Parse(correlationId),
                metadata,
                inTransaction.Id);

            outTransaction.Complete();
            inTransaction.Complete();

            // Save changes
            _context.Wallets.UpdateRange(fromWallet, toWallet);
            await _context.Transactions.AddRangeAsync(new[] { outTransaction, inTransaction }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Transfer of {Amount} completed from wallet {FromWalletId} to wallet {ToWalletId} with transactions {OutTransactionId}/{InTransactionId}", 
                amount, fromWalletId, toWalletId, outTransaction.Id, inTransaction.Id);

            return (outTransaction, inTransaction);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing transfer from wallet {FromWalletId} to wallet {ToWalletId}", fromWalletId, toWalletId);
            throw;
        }
    }

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
    }
}