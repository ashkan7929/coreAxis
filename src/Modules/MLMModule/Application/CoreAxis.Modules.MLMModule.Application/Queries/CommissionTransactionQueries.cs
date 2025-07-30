using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Queries;

public class GetCommissionByIdQuery : IRequest<CommissionTransactionDto?>
{
    public Guid CommissionId { get; set; }
}

public class GetUserCommissionsQuery : IRequest<IEnumerable<CommissionTransactionDto>>
{
    public Guid UserId { get; set; }
    public CommissionFilterDto Filter { get; set; } = new();
}

public class GetCommissionsByStatusQuery : IRequest<IEnumerable<CommissionTransactionDto>>
{
    public CommissionStatus Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetCommissionsBySourcePaymentQuery : IRequest<IEnumerable<CommissionTransactionDto>>
{
    public Guid SourcePaymentId { get; set; }
}

public class GetCommissionSummaryQuery : IRequest<CommissionSummaryDto>
{
    public Guid UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class GetPendingCommissionsForApprovalQuery : IRequest<IEnumerable<CommissionTransactionDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetCommissionsByDateRangeQuery : IRequest<IEnumerable<CommissionTransactionDto>>
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? UserId { get; set; }
    public CommissionStatus? Status { get; set; }
}