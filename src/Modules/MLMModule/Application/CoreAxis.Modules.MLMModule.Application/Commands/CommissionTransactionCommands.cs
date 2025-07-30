using CoreAxis.Modules.MLMModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Commands;

public class ApproveCommissionCommand : IRequest<CommissionTransactionDto>
{
    public Guid CommissionId { get; set; }
    public Guid ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

public class RejectCommissionCommand : IRequest<CommissionTransactionDto>
{
    public Guid CommissionId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public Guid RejectedBy { get; set; }
    public string? Notes { get; set; }
}

public class MarkCommissionAsPaidCommand : IRequest<CommissionTransactionDto>
{
    public Guid CommissionId { get; set; }
    public Guid WalletTransactionId { get; set; }
    public Guid PaidBy { get; set; }
    public string? Notes { get; set; }
}

public class ProcessPendingCommissionsCommand : IRequest<List<CommissionTransactionDto>>
{
    public Guid ProcessedBy { get; set; }
    public int BatchSize { get; set; } = 50;
    public string? Notes { get; set; }
}

public class UpdateCommissionNotesCommand : IRequest<CommissionTransactionDto>
{
    public Guid CommissionId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class ExpireCommissionCommand : IRequest<CommissionTransactionDto>
{
    public Guid CommissionId { get; set; }
}