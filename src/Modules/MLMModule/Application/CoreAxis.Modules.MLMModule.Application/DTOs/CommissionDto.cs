using CoreAxis.Modules.MLMModule.Domain.Enums;

namespace CoreAxis.Modules.MLMModule.Application.DTOs;

public class CommissionTransactionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SourcePaymentId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid CommissionRuleSetId { get; set; }
    public int Level { get; set; }
    public decimal Amount { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal Percentage { get; set; }
    public CommissionStatus Status { get; set; }
    public bool IsSettled { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionNotes { get; set; }
}

public class CommissionSummaryDto
{
    public decimal TotalEarnings { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int TotalTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public int PaidTransactions { get; set; }
}

public class CommissionFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public CommissionStatus? Status { get; set; }
    public int? Level { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class MarkCommissionAsPaidDto
{
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
}

public class ProcessPendingCommissionsDto
{
    public int BatchSize { get; set; } = 50;
    public decimal? MinimumAmount { get; set; }
    public DateTime? CutoffDate { get; set; }
    public string? Notes { get; set; }
}

public class ProcessPendingCommissionsResultDto
{
    public int ProcessedCount { get; set; }
    public List<CommissionTransactionDto> ProcessedCommissions { get; set; } = new();
}

public class UpdateCommissionNotesDto
{
    public string Notes { get; set; } = string.Empty;
}

public class ExpireCommissionDto
{
    public string? ExpirationReason { get; set; }
}

public class ApproveCommissionDto
{
    public string? ApprovalNotes { get; set; }
}

public class RejectCommissionDto
{
    public string RejectionReason { get; set; } = string.Empty;
    public string? RejectionNotes { get; set; }
}

public class ApproveCommissionRequest
{
    public string? Notes { get; set; }
}

public class RejectCommissionRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class GetCommissionsRequest
{
    public Guid? UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class CreateCommissionRuleSetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CommissionCalculationDto
{
    public Guid UserId { get; set; }
    public int Level { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public decimal SourceAmount { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}