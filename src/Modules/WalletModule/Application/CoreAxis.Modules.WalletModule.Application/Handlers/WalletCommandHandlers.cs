using CoreAxis.Modules.WalletModule.Application.Commands;
using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
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
        var wallet = new Wallet(request.UserId, request.WalletTypeId, request.TenantId, request.Currency);
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
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<DepositCommandHandler> _logger;

    public DepositCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<DepositCommandHandler> logger)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionResultDto> Handle(DepositCommand request, CancellationToken cancellationToken)
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

            // Get deposit transaction type
            var transactionType = await _transactionTypeRepository.GetByCodeAsync("DEPOSIT", request.TenantId, cancellationToken);
            if (transactionType == null)
            {
                return new TransactionResultDto
                {
                    Success = false,
                    Message = "Transaction type not found",
                    Errors = ["DEPOSIT transaction type not configured"]
                };
            }

            // Credit wallet
            wallet.Credit(request.Amount, request.Description);

            // Create transaction record
            var transaction = new Transaction(
                wallet.Id,
                transactionType.Id,
                request.Amount,
                wallet.Balance,
                request.Description,
                request.TenantId,
                request.Reference,
                request.Metadata);

            transaction.Complete();

            // Save changes
            await _walletRepository.UpdateAsync(wallet, cancellationToken);
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            _logger.LogInformation("Deposit of {Amount} completed for wallet {WalletId}", request.Amount, request.WalletId);

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
                    ProcessedAt = transaction.ProcessedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit for wallet {WalletId}", request.WalletId);
            return new TransactionResultDto
            {
                Success = false,
                Message = "Deposit failed",
                Errors = [ex.Message]
            };
        }
    }
}