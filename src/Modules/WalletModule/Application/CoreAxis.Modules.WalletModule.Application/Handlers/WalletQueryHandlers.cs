using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Queries;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Application.Handlers;

public class GetWalletByIdQueryHandler : IRequestHandler<GetWalletByIdQuery, WalletDto?>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly ILogger<GetWalletByIdQueryHandler> _logger;

    public GetWalletByIdQueryHandler(
        IWalletRepository walletRepository,
        IWalletTypeRepository walletTypeRepository,
        ILogger<GetWalletByIdQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _walletTypeRepository = walletTypeRepository;
        _logger = logger;
    }

    public async Task<WalletDto?> Handle(GetWalletByIdQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
        if (wallet == null)
        {
            return null;
        }

        var walletType = await _walletTypeRepository.GetByIdAsync(wallet.WalletTypeId, cancellationToken);

        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            WalletTypeId = wallet.WalletTypeId,
            WalletTypeName = walletType?.Name ?? "Unknown",
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            IsLocked = wallet.IsLocked,
            LockReason = wallet.LockReason,
            CreatedOn = wallet.CreatedOn
        };
    }
}

public class GetUserWalletsQueryHandler : IRequestHandler<GetUserWalletsQuery, IEnumerable<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly ILogger<GetUserWalletsQueryHandler> _logger;

    public GetUserWalletsQueryHandler(
        IWalletRepository walletRepository,
        IWalletTypeRepository walletTypeRepository,
        ILogger<GetUserWalletsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _walletTypeRepository = walletTypeRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<WalletDto>> Handle(GetUserWalletsQuery request, CancellationToken cancellationToken)
    {
        var wallets = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var walletTypes = await _walletTypeRepository.GetAllAsync(cancellationToken);
        var walletTypeDict = walletTypes.ToDictionary(wt => wt.Id, wt => wt.Name);

        return wallets
            .Select(wallet => new WalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                WalletTypeId = wallet.WalletTypeId,
                WalletTypeName = walletTypeDict.GetValueOrDefault(wallet.WalletTypeId, "Unknown"),
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                IsLocked = wallet.IsLocked,
                LockReason = wallet.LockReason,
                CreatedOn = wallet.CreatedOn
            });
    }
}

public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, WalletBalanceDto?>
{
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<GetWalletBalanceQueryHandler> _logger;

    public GetWalletBalanceQueryHandler(
        IWalletRepository walletRepository,
        ILogger<GetWalletBalanceQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _logger = logger;
    }

    public async Task<WalletBalanceDto?> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken);
        if (wallet == null)
        {
            return null;
        }

        return new WalletBalanceDto
        {
            WalletId = wallet.Id,
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            LastUpdated = wallet.LastModifiedOn ?? wallet.CreatedOn
        };
    }
}

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, IEnumerable<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<GetTransactionsQueryHandler> _logger;

    public GetTransactionsQueryHandler(
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<GetTransactionsQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<CoreAxis.Modules.WalletModule.Domain.Entities.Transaction> transactions;

        if (request.Filter.FromDate.HasValue && request.Filter.ToDate.HasValue)
        {
            transactions = await _transactionRepository.GetByDateRangeAsync(
                request.Filter.FromDate.Value,
                request.Filter.ToDate.Value,
                request.Filter.UserId,
                cancellationToken);
        }
        else if (request.Filter.WalletId.HasValue)
        {
            transactions = await _transactionRepository.GetByWalletIdAsync(request.Filter.WalletId.Value, cancellationToken);
        }
        else if (request.Filter.UserId.HasValue)
        {
            transactions = await _transactionRepository.GetByUserIdAsync(request.Filter.UserId.Value, cancellationToken);
        }
        else
        {
            // Get all transactions with pagination logic if needed
            transactions = new List<CoreAxis.Modules.WalletModule.Domain.Entities.Transaction>();
        }

        // Filter by transaction type if specified
        if (request.Filter.TransactionTypeId.HasValue)
        {
            transactions = transactions.Where(t => t.TransactionTypeId == request.Filter.TransactionTypeId.Value);
        }

        // Filter by status if specified
        if (request.Filter.Status.HasValue)
        {
            transactions = transactions.Where(t => t.Status == request.Filter.Status.Value);
        }

        // Get transaction types for mapping
        var transactionTypes = await _transactionTypeRepository.GetAllAsync(cancellationToken);
        var transactionTypeDict = transactionTypes.ToDictionary(tt => tt.Id, tt => new { tt.Name, tt.Code });

        return transactions
            .Select(transaction => new TransactionDto
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                TransactionTypeId = transaction.TransactionTypeId,
                TransactionTypeName = transactionTypeDict.GetValueOrDefault(transaction.TransactionTypeId)?.Name ?? "Unknown",
                TransactionTypeCode = transactionTypeDict.GetValueOrDefault(transaction.TransactionTypeId)?.Code ?? "UNKNOWN",
                Amount = transaction.Amount,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                Reference = transaction.Reference,
                Status = transaction.Status,
                ProcessedAt = transaction.ProcessedAt,
                RelatedTransactionId = transaction.RelatedTransactionId
            })
            .OrderByDescending(t => t.ProcessedAt);
    }
}

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto?>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly ILogger<GetTransactionByIdQueryHandler> _logger;

    public GetTransactionByIdQueryHandler(
        ITransactionRepository transactionRepository,
        ITransactionTypeRepository transactionTypeRepository,
        ILogger<GetTransactionByIdQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _logger = logger;
    }

    public async Task<TransactionDto?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction == null)
        {
            return null;
        }

        var transactionType = await _transactionTypeRepository.GetByIdAsync(transaction.TransactionTypeId, cancellationToken);

        return new TransactionDto
        {
            Id = transaction.Id,
            WalletId = transaction.WalletId,
            TransactionTypeId = transaction.TransactionTypeId,
            TransactionTypeName = transactionType?.Name ?? "Unknown",
            TransactionTypeCode = transactionType?.Code ?? "UNKNOWN",
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            Reference = transaction.Reference,
            Status = transaction.Status,
            ProcessedAt = transaction.ProcessedAt,
            RelatedTransactionId = transaction.RelatedTransactionId
        };
    }
}