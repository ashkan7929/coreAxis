using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly WalletDbContext _context;
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<TransactionService> _logger;
    private readonly IWalletPolicyService _walletPolicyService;

    // Metrics
    private static readonly Meter _meter = new("CoreAxis.Wallet", "1.0");
    private static readonly Counter<long> _creditsCounter = _meter.CreateCounter<long>("wallet.credits.count");
    private static readonly Counter<long> _debitsCounter = _meter.CreateCounter<long>("wallet.debits.count");
    private static readonly Counter<long> _transfersCounter = _meter.CreateCounter<long>("wallet.transfers.count");
    private static readonly Counter<long> _failuresCounter = _meter.CreateCounter<long>("wallet.failures.count");
    private static readonly Histogram<double> _latencyMs = _meter.CreateHistogram<double>("wallet.operations.latency.ms");

    public TransactionService(
        WalletDbContext context,
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<TransactionService> logger,
        IWalletPolicyService walletPolicyService)
    {
        _context = context;
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
        _walletPolicyService = walletPolicyService;
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
        var sw = Stopwatch.StartNew();
        // Check for existing transaction with same idempotency key
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTransaction = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingTransaction != null)
            {
                _logger.LogInformation("Returning existing transaction for idempotency key: {IdempotencyKey}", idempotencyKey);
                _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "deposit"));
                return existingTransaction;
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get wallet with row-level locking
            var wallet = await _context.Wallets
                .Include(w => w.WalletType)
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

            // Enforce lock: deposits to locked wallets are blocked unless policy allows
            if (wallet.IsLocked)
            {
                _logger.LogWarning("SECURITY: Attempted deposit on locked wallet {WalletId}. UserId {UserId}. Reason {LockReason}. Code {ErrorCode}",
                    walletId, wallet.UserId, wallet.LockReason, "WLT_ACCOUNT_FROZEN");
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_ACCOUNT_FROZEN"));
                throw new InvalidOperationException($"Wallet is locked: {wallet.LockReason}");
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

            _creditsCounter.Add(1, new KeyValuePair<string, object?>("type", "DEPOSIT"));
            _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "deposit"));

            return newTransaction;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing deposit for wallet {WalletId}", walletId);
            _failuresCounter.Add(1, new KeyValuePair<string, object?>("operation", "deposit"));
            throw;
        }
    }

    public async Task<Transaction> ExecuteCommissionCreditAsync(
        Guid walletId,
        decimal amount,
        string description,
        string? reference = null,
        object? metadata = null,
        string? idempotencyKey = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        // Check for existing transaction with same idempotency key
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTransaction = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingTransaction != null)
            {
                _logger.LogInformation("Returning existing commission transaction for idempotency key: {IdempotencyKey}", idempotencyKey);
                _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "commission"));
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

            // Check if wallet is locked
            if (wallet.IsLocked)
            {
                _logger.LogWarning("SECURITY: Attempted commission credit on locked wallet {WalletId}. UserId {UserId}. Reason {LockReason}. Code {ErrorCode}",
                    walletId, wallet.UserId, wallet.LockReason, "WLT_ACCOUNT_FROZEN");
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_ACCOUNT_FROZEN"));
                throw new InvalidOperationException($"Wallet is locked: {wallet.LockReason}");
            }

            // Get commission transaction type
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("COMMISSION", cancellationToken);
            if (transactionType == null)
            {
                throw new InvalidOperationException("COMMISSION transaction type not configured");
            }

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
                null);

            // Save changes
            _context.Wallets.Update(wallet);
            await _context.Transactions.AddAsync(newTransaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Commission credit of {Amount} completed for wallet {WalletId} with transaction {TransactionId}",
                amount, walletId, newTransaction.Id);

            _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "commission"));

            return newTransaction;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing commission credit for wallet {WalletId}", walletId);
            _failuresCounter.Add(1, new KeyValuePair<string, object?>("operation", "commission"));
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
        var sw = Stopwatch.StartNew();
        // Check for existing transaction with same idempotency key
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTransaction = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingTransaction != null)
            {
                _logger.LogInformation("Returning existing transaction for idempotency key: {IdempotencyKey}", idempotencyKey);
                _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "withdrawal"));
                return existingTransaction;
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get wallet with row-level locking
            var wallet = await _context.Wallets
                .Include(w => w.WalletType)
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

            // Policy enforcement
            var policy = await _walletPolicyService.GetPolicyAsync("default", wallet.WalletType.Currency, cancellationToken);

            // Enforce lock: withdrawals from locked wallets are blocked
            if (wallet.IsLocked)
            {
                _logger.LogWarning("SECURITY: Attempted withdrawal on locked wallet {WalletId}. UserId {UserId}. Reason {LockReason}. Code {ErrorCode}",
                    walletId, wallet.UserId, wallet.LockReason, "WLT_ACCOUNT_FROZEN");
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_ACCOUNT_FROZEN"));
                throw new InvalidOperationException($"Wallet is locked: {wallet.LockReason}");
            }

            // Negative balance rule
            if (!policy.AllowNegative && !wallet.CanDebit(amount))
            {
                _logger.LogWarning("POLICY: Withdrawal blocked due to insufficient balance. walletId={WalletId} code=WLT_NEGATIVE_BLOCKED",
                    walletId);
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_NEGATIVE_BLOCKED"));
                throw new InvalidOperationException("Insufficient balance");
            }

            // Daily debit cap
            if (policy.DailyDebitCap.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                var debitedToday = await _context.Transactions
                    .Where(t => t.WalletId == walletId && t.Amount < 0 && t.CreatedOn >= today)
                    .SumAsync(t => -t.Amount, cancellationToken);
                if (debitedToday + amount > policy.DailyDebitCap.Value)
                {
                    _logger.LogWarning("POLICY: Daily debit cap exceeded. walletId={WalletId} attempted={Attempted} debitedToday={DebitedToday} cap={Cap} code=WLT_POLICY_VIOLATION",
                        walletId, amount, debitedToday, policy.DailyDebitCap.Value);
                    _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_POLICY_VIOLATION"));
                    throw new InvalidOperationException("Daily debit cap exceeded");
                }
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

            _debitsCounter.Add(1, new KeyValuePair<string, object?>("type", "WITHDRAW"));
            _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "withdrawal"));

            return newTransaction;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing withdrawal for wallet {WalletId}", walletId);
            _failuresCounter.Add(1, new KeyValuePair<string, object?>("operation", "withdrawal"));
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
        var sw = Stopwatch.StartNew();
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
                    _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "transfer"));
                    return (existingTransaction, relatedTransaction);
                }
                else
                {
                    _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "transfer"));
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
                .Include(w => w.WalletType)
                .Where(w => walletIds.Contains(w.Id))
                .OrderBy(w => w.Id)
                .ToListAsync(cancellationToken);

            var fromWallet = wallets.FirstOrDefault(w => w.Id == fromWalletId);
            var toWallet = wallets.FirstOrDefault(w => w.Id == toWalletId);

            if (fromWallet == null || toWallet == null)
            {
                throw new InvalidOperationException("One or both wallets not found");
            }

            // Transfer invariants
            if (fromWallet.Id == toWallet.Id)
            {
                throw new InvalidOperationException("Invalid transfer: source and destination are the same");
            }
            if (!string.Equals(fromWallet.WalletType.Currency, toWallet.WalletType.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid transfer: currency mismatch");
            }

            // Validate amount
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            // Policy enforcement on source wallet
            var fromPolicy = await _walletPolicyService.GetPolicyAsync("default", fromWallet.WalletType.Currency, cancellationToken);

            if (fromWallet.IsLocked)
            {
                _logger.LogWarning("SECURITY: Attempted transfer from locked source wallet {FromWalletId}. UserId {UserId}. Reason {LockReason}. Code {ErrorCode}",
                    fromWalletId, fromWallet.UserId, fromWallet.LockReason, "WLT_ACCOUNT_FROZEN");
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_ACCOUNT_FROZEN"));
                throw new InvalidOperationException($"Source wallet is locked: {fromWallet.LockReason}");
            }

            if (!fromPolicy.AllowNegative && !fromWallet.CanDebit(amount))
            {
                _logger.LogWarning("POLICY: Transfer blocked due to insufficient balance. fromWalletId={FromWalletId} code=WLT_NEGATIVE_BLOCKED",
                    fromWalletId);
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_NEGATIVE_BLOCKED"));
                throw new InvalidOperationException("Insufficient balance");
            }

            if (fromPolicy.DailyDebitCap.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                var debitedToday = await _context.Transactions
                    .Where(t => t.WalletId == fromWalletId && t.Amount < 0 && t.CreatedOn >= today)
                    .SumAsync(t => -t.Amount, cancellationToken);
                if (debitedToday + amount > fromPolicy.DailyDebitCap.Value)
                {
                    _logger.LogWarning("POLICY: Daily debit cap exceeded for transfer. fromWalletId={FromWalletId} attempted={Attempted} debitedToday={DebitedToday} cap={Cap} code=WLT_POLICY_VIOLATION",
                        fromWalletId, amount, debitedToday, fromPolicy.DailyDebitCap.Value);
                    _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_POLICY_VIOLATION"));
                    throw new InvalidOperationException("Daily debit cap exceeded");
                }
            }

            // Check if destination wallet is locked
            if (toWallet.IsLocked)
            {
                _logger.LogWarning("SECURITY: Attempted transfer to locked destination wallet {ToWalletId}. UserId {UserId}. Reason {LockReason}. Code {ErrorCode}",
                    toWalletId, toWallet.UserId, toWallet.LockReason, "WLT_ACCOUNT_FROZEN");
                _failuresCounter.Add(1, new KeyValuePair<string, object?>("code", "WLT_ACCOUNT_FROZEN"));
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

            _transfersCounter.Add(1, new KeyValuePair<string, object?>("type", "TRANSFER"));
            _latencyMs.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "transfer"));

            return (outTransaction, inTransaction);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing transfer from wallet {FromWalletId} to wallet {ToWalletId}", fromWalletId, toWalletId);
            _failuresCounter.Add(1, new KeyValuePair<string, object?>("operation", "transfer"));
            throw;
        }
    }

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
    }
}