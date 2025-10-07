using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Commands;
using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Application.Handlers;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, TransactionResultDto>
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<WithdrawCommandHandler> _logger;

    public WithdrawCommandHandler(
        ITransactionService transactionService,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<WithdrawCommandHandler> logger)
    {
        _transactionService = transactionService;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionResultDto> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        try
        {
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

            // Get withdrawal transaction type for response
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("WITHDRAW", cancellationToken);
            if (transactionType == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Transaction type not found",
                    Errors = ["WITHDRAW transaction type not configured"]
                };
            }

            // Execute withdrawal using atomic transaction service
            var transaction = await _transactionService.ExecuteWithdrawalAsync(
                request.WalletId,
                request.Amount,
                request.Description,
                request.Reference,
                request.Metadata,
                request.IdempotencyKey,
                request.CorrelationId,
                cancellationToken);

            _logger.LogInformation("Withdrawal of {Amount} completed for wallet {WalletId} with transaction {TransactionId}", 
                request.Amount, request.WalletId, transaction.Id);

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
                    ProcessedAt = transaction.ProcessedAt,
                    IdempotencyKey = transaction.IdempotencyKey,
                    CorrelationId = transaction.CorrelationId
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation for withdrawal on wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message],
                Code = MapErrorCode(ex.Message)
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for withdrawal on wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message],
                Code = MapErrorCode(ex.Message)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal for wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = "Withdrawal failed",
                Errors = ["An unexpected error occurred"],
                Code = "WLT_CONCURRENCY_CONFLICT"
            };
        }
    }

    private static string MapErrorCode(string message)
    {
        if (message.Contains("Insufficient balance", StringComparison.OrdinalIgnoreCase))
            return "WLT_NEGATIVE_BLOCKED";
        if (message.Contains("locked", StringComparison.OrdinalIgnoreCase))
            return "WLT_ACCOUNT_FROZEN";
        if (message.Contains("Amount must be positive", StringComparison.OrdinalIgnoreCase))
            return "WLT_POLICY_VIOLATION";
        if (message.Contains("currency mismatch", StringComparison.OrdinalIgnoreCase) || message.Contains("source and destination are the same", StringComparison.OrdinalIgnoreCase))
            return "WLT_INVALID_TRANSFER";
        return "WLT_POLICY_VIOLATION";
    }
}

public class TransferCommandHandler : IRequestHandler<TransferCommand, TransactionResultDto>
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<TransferCommandHandler> _logger;

    public TransferCommandHandler(
        ITransactionService transactionService,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<TransferCommandHandler> logger)
    {
        _transactionService = transactionService;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionResultDto> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        try
        {
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

            // Get transfer transaction type for response
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("TRANSFER", cancellationToken);
            if (transactionType == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Transaction type not found",
                    Errors = ["TRANSFER transaction type not configured"]
                };
            }

            // Execute transfer using atomic transaction service
            var (debitTransaction, creditTransaction) = await _transactionService.ExecuteTransferAsync(
                request.FromWalletId,
                request.ToWalletId,
                request.Amount,
                request.Description,
                request.Reference,
                request.Metadata,
                request.IdempotencyKey,
                request.CorrelationId,
                cancellationToken);

            _logger.LogInformation("Transfer of {Amount} completed from wallet {FromWalletId} to wallet {ToWalletId} with transactions {DebitTransactionId} and {CreditTransactionId}", 
                request.Amount, request.FromWalletId, request.ToWalletId, debitTransaction.Id, creditTransaction.Id);

            return new TransactionResultDto
            {
                Success = true,
                Message = "Transfer completed successfully",
                Transaction = new TransactionDto
                {
                    Id = debitTransaction.Id,
                    WalletId = debitTransaction.WalletId,
                    TransactionTypeId = debitTransaction.TransactionTypeId,
                    TransactionTypeName = transactionType.Name,
                    TransactionTypeCode = transactionType.Code,
                    Amount = debitTransaction.Amount,
                    BalanceAfter = debitTransaction.BalanceAfter,
                    Description = debitTransaction.Description,
                    Reference = debitTransaction.Reference,
                    Status = debitTransaction.Status,
                    ProcessedAt = debitTransaction.ProcessedAt,
                    IdempotencyKey = debitTransaction.IdempotencyKey,
                    CorrelationId = debitTransaction.CorrelationId
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation for transfer from wallet {FromWalletId} to wallet {ToWalletId}", 
                request.FromWalletId, request.ToWalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message],
                Code = MapErrorCode(ex.Message)
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for transfer from wallet {FromWalletId} to wallet {ToWalletId}", 
                request.FromWalletId, request.ToWalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message],
                Code = MapErrorCode(ex.Message)
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
                Errors = ["An unexpected error occurred"],
                Code = "WLT_CONCURRENCY_CONFLICT"
            };
        }
    }

    private static string MapErrorCode(string message)
    {
        if (message.Contains("Insufficient balance", StringComparison.OrdinalIgnoreCase))
            return "WLT_NEGATIVE_BLOCKED";
        if (message.Contains("locked", StringComparison.OrdinalIgnoreCase))
            return "WLT_ACCOUNT_FROZEN";
        if (message.Contains("Amount must be positive", StringComparison.OrdinalIgnoreCase))
            return "WLT_POLICY_VIOLATION";
        if (message.Contains("currency mismatch", StringComparison.OrdinalIgnoreCase) || message.Contains("source and destination are the same", StringComparison.OrdinalIgnoreCase))
            return "WLT_INVALID_TRANSFER";
        return "WLT_POLICY_VIOLATION";
    }
}