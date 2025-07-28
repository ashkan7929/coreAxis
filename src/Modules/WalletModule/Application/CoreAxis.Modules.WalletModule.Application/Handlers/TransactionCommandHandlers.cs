using CoreAxis.Modules.WalletModule.Application.Commands;
using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Application.Handlers;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, TransactionResultDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<WithdrawCommandHandler> _logger;

    public WithdrawCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<WithdrawCommandHandler> logger)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionResultDto> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get wallet
            var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
            if (wallet == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Wallet not found",
                    Errors = ["Invalid wallet ID"]
                };
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Invalid amount",
                    Errors = ["Amount must be positive"]
                };
            }

            // Check if wallet can be debited
            if (!wallet.CanDebit(request.Amount))
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Insufficient balance or wallet is locked",
                    Errors = [wallet.IsLocked ? $"Wallet is locked: {wallet.LockReason}" : "Insufficient balance"]
                };
            }

            // Get withdraw transaction type
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("WITHDRAW", request.TenantId, cancellationToken);
            if (transactionType == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Transaction type not found",
                    Errors = ["WITHDRAW transaction type not configured"]
                };
            }

            // Debit wallet
            wallet.Debit(request.Amount, request.Description);

            // Create transaction record
            var transaction = new Transaction(
                wallet.Id,
                transactionType.Id,
                -request.Amount, // Negative for withdrawal
                wallet.Balance,
                request.Description,
                request.TenantId,
                request.Reference,
                request.Metadata);

            transaction.Complete();

            // Save changes
            await _walletRepository.UpdateAsync(wallet, cancellationToken);
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            _logger.LogInformation("Withdrawal of {Amount} completed for wallet {WalletId}", request.Amount, request.WalletId);

            return new TransactionResultDto
            {
                Success = true,
                Message = "Withdrawal completed successfully",
                Transaction = new TransactionDto
                {
                    Id = transaction.Id,
                    WalletId = transaction.WalletId,
                    TransactionTypeId = transaction.TransactionTypeId,
                    TransactionTypeName = transactionType.Name,
                    TransactionTypeCode = transactionType.Code,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    Reference = transaction.Reference,
                    Status = transaction.Status,
                    ProcessedAt = transaction.ProcessedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal for wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = "Withdrawal failed",
                Errors = [ex.Message]
            };
        }
    }
}

public class TransferCommandHandler : IRequestHandler<TransferCommand, TransactionResultDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<TransferCommandHandler> _logger;

    public TransferCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<TransferCommandHandler> logger)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionResultDto> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get both wallets
            var fromWallet = await _walletRepository.GetByIdAsync(request.FromWalletId, cancellationToken);
            var toWallet = await _walletRepository.GetByIdAsync(request.ToWalletId, cancellationToken);

            if (fromWallet == null || toWallet == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "One or both wallets not found",
                    Errors = ["Invalid wallet IDs"]
                };
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Invalid amount",
                    Errors = ["Amount must be positive"]
                };
            }

            // Check if source wallet can be debited
            if (!fromWallet.CanDebit(request.Amount))
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Insufficient balance or source wallet is locked",
                    Errors = [fromWallet.IsLocked ? $"Source wallet is locked: {fromWallet.LockReason}" : "Insufficient balance"]
                };
            }

            // Check if destination wallet is locked
            if (toWallet.IsLocked)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Destination wallet is locked",
                    Errors = [$"Destination wallet is locked: {toWallet.LockReason}"]
                };
            }

            // Get transfer transaction types
            var transferOutType = await _transactionTypeRepository.GetByCodeAsync("TRANSFER_OUT", request.TenantId, cancellationToken);
            var transferInType = await _transactionTypeRepository.GetByCodeAsync("TRANSFER_IN", request.TenantId, cancellationToken);

            if (transferOutType == null || transferInType == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Transfer transaction types not found",
                    Errors = ["TRANSFER_OUT or TRANSFER_IN transaction types not configured"]
                };
            }

            // Perform transfer
            fromWallet.Debit(request.Amount, request.Description);
            toWallet.Credit(request.Amount, request.Description);

            // Create transaction records
            var outTransaction = new Transaction(
                fromWallet.Id,
                transferOutType.Id,
                -request.Amount,
                fromWallet.Balance,
                request.Description,
                request.TenantId,
                request.Reference,
                request.Metadata);

            var inTransaction = new Transaction(
                toWallet.Id,
                transferInType.Id,
                request.Amount,
                toWallet.Balance,
                request.Description,
                request.TenantId,
                request.Reference,
                request.Metadata,
                outTransaction.Id);

            // Link transactions
            outTransaction = new Transaction(
                fromWallet.Id,
                transferOutType.Id,
                -request.Amount,
                fromWallet.Balance,
                request.Description,
                request.TenantId,
                request.Reference,
                request.Metadata,
                inTransaction.Id);

            outTransaction.Complete();
            inTransaction.Complete();

            // Save changes
            await _walletRepository.UpdateAsync(fromWallet, cancellationToken);
            await _walletRepository.UpdateAsync(toWallet, cancellationToken);
            await _transactionRepository.AddAsync(outTransaction, cancellationToken);
            await _transactionRepository.AddAsync(inTransaction, cancellationToken);

            _logger.LogInformation("Transfer of {Amount} completed from wallet {FromWalletId} to wallet {ToWalletId}", 
                request.Amount, request.FromWalletId, request.ToWalletId);

            return new TransactionResultDto
            {
                Success = true,
                Message = "Transfer completed successfully",
                Transaction = new TransactionDto
                {
                    Id = outTransaction.Id,
                    WalletId = outTransaction.WalletId,
                    TransactionTypeId = outTransaction.TransactionTypeId,
                    TransactionTypeName = transferOutType.Name,
                    TransactionTypeCode = transferOutType.Code,
                    Amount = outTransaction.Amount,
                    BalanceAfter = outTransaction.BalanceAfter,
                    Description = outTransaction.Description,
                    Reference = outTransaction.Reference,
                    Status = outTransaction.Status,
                    ProcessedAt = outTransaction.ProcessedAt,
                    RelatedTransactionId = outTransaction.RelatedTransactionId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transfer from wallet {FromWalletId} to wallet {ToWalletId}", 
                request.FromWalletId, request.ToWalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = "Transfer failed",
                Errors = [ex.Message]
            };
        }
    }
}