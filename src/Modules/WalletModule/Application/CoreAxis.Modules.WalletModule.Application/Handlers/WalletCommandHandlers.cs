using CoreAxis.Modules.WalletModule.Application.Commands;
using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Application.Handlers;

public class CreateWalletCommandHandler : IRequestHandler<CreateWalletCommand, WalletDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly ILogger<CreateWalletCommandHandler> _logger;

    public CreateWalletCommandHandler(
        IWalletRepository walletRepository,
        IWalletTypeRepository walletTypeRepository,
        ILogger<CreateWalletCommandHandler> logger)
    {
        _walletRepository = walletRepository;
        _walletTypeRepository = walletTypeRepository;
        _logger = logger;
    }

    public async Task<WalletDto> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        // Check if wallet already exists
        var existingWallet = await _walletRepository.GetByUserAndTypeAsync(request.UserId, request.WalletTypeId, cancellationToken);
        if (existingWallet != null)
        {
            throw new InvalidOperationException("Wallet already exists for this user and wallet type");
        }

        // Validate wallet type exists
        var walletType = await _walletTypeRepository.GetByIdAsync(request.WalletTypeId, cancellationToken);
        if (walletType == null)
        {
            throw new ArgumentException("Invalid wallet type", nameof(request.WalletTypeId));
        }

        // Create new wallet
        var wallet = new Wallet(request.UserId, request.WalletTypeId, request.Currency);
        await _walletRepository.AddAsync(wallet, cancellationToken);

        _logger.LogInformation("Wallet created for user {UserId} with type {WalletTypeId}", request.UserId, request.WalletTypeId);

        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            WalletTypeId = wallet.WalletTypeId,
            WalletTypeName = walletType.Name,
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            IsLocked = wallet.IsLocked,
            LockReason = wallet.LockReason,
            CreatedOn = wallet.CreatedOn
        };
    }
}

public class DepositCommandHandler : IRequestHandler<DepositCommand, TransactionResultDto>
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<DepositCommandHandler> _logger;

    public DepositCommandHandler(
        ITransactionService transactionService,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<DepositCommandHandler> logger)
    {
        _transactionService = transactionService;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionResultDto> Handle(DepositCommand request, CancellationToken cancellationToken)
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

            // Get deposit transaction type for response
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("DEPOSIT", cancellationToken);
            if (transactionType == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Transaction type not found",
                    Errors = ["DEPOSIT transaction type not configured"]
                };
            }

            // Execute deposit using atomic transaction service
            var transaction = await _transactionService.ExecuteDepositAsync(
                request.WalletId,
                request.Amount,
                request.Description,
                request.Reference,
                request.Metadata,
                request.IdempotencyKey,
                request.CorrelationId,
                cancellationToken);

            _logger.LogInformation("Deposit of {Amount} completed for wallet {WalletId} with transaction {TransactionId}", 
                request.Amount, request.WalletId, transaction.Id);

            return new TransactionResultDto
            {
                Success = true,
                Message = "Deposit completed successfully",
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
            _logger.LogWarning(ex, "Business rule violation for deposit on wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message]
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deposit on wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit for wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = "Deposit failed",
                Errors = ["An unexpected error occurred"]
            };
        }
    }
}