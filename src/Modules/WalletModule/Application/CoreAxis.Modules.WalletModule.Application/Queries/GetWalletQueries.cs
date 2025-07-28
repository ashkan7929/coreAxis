using CoreAxis.Modules.WalletModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.WalletModule.Application.Queries;

public class GetWalletByIdQuery : IRequest<WalletDto?>
{
    public Guid WalletId { get; set; }
    public Guid TenantId { get; set; }
}

public class GetUserWalletsQuery : IRequest<IEnumerable<WalletDto>>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
}

public class GetWalletBalanceQuery : IRequest<WalletBalanceDto?>
{
    public Guid WalletId { get; set; }
    public Guid TenantId { get; set; }
}

public class GetTransactionsQuery : IRequest<IEnumerable<TransactionDto>>
{
    public TransactionFilterDto Filter { get; set; } = new();
    public Guid TenantId { get; set; }
}

public class GetTransactionByIdQuery : IRequest<TransactionDto?>
{
    public Guid TransactionId { get; set; }
    public Guid TenantId { get; set; }
}