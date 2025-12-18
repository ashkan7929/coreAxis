using CoreAxis.Modules.WalletModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.WalletModule.Application.Commands;

public class CreateWalletCommand : IRequest<WalletDto>
{
    public Guid UserId { get; set; }
    public Guid WalletTypeId { get; set; }
}

public class DepositCommand : IRequest<TransactionResultDto>
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public object? Metadata { get; set; }
    public Guid UserId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? CorrelationId { get; set; }
}

public class WithdrawCommand : IRequest<TransactionResultDto>
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public object? Metadata { get; set; }
    public Guid UserId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? CorrelationId { get; set; }
}

public class TransferCommand : IRequest<TransactionResultDto>
{
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public object? Metadata { get; set; }
    public Guid UserId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? CorrelationId { get; set; }
}