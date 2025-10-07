using CoreAxis.Modules.WalletModule.Domain.Entities;

namespace CoreAxis.Modules.WalletModule.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid TransactionTypeId { get; set; }
    public string TransactionTypeName { get; set; } = string.Empty;
    public string TransactionTypeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? CorrelationId { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Guid? RelatedTransactionId { get; set; }
}

public class DepositRequestDto
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public object? Metadata { get; set; }
}

public class WithdrawRequestDto
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public object? Metadata { get; set; }
}

public class TransferRequestDto
{
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public object? Metadata { get; set; }
}

public class TransactionFilterDto
{
    public Guid? WalletId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TransactionTypeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TransactionStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    // Optional cursor-based pagination (createdAt+Id encoded)
    public string? Cursor { get; set; }
    public int? Limit { get; set; }
}

public class TransactionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TransactionDto? Transaction { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Code { get; set; }
}