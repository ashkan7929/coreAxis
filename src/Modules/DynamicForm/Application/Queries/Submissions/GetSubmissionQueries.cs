using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.Submissions;

public record GetSubmissionByIdQuery : IRequest<Result<FormSubmissionDto>>
{
    public Guid Id { get; init; }
    public bool IncludeForm { get; init; } = false;
}

public record GetSubmissionsQuery : IRequest<Result<PagedResult<FormSubmissionDto>>>
{
    public string TenantId { get; init; } = default!;
    public Guid? FormId { get; init; }
    public string? UserId { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool IncludeForm { get; init; } = false;
}

public record GetSubmissionsByFormQuery : IRequest<Result<PagedResult<FormSubmissionDto>>>
{
    public string TenantId { get; init; } = default!;
    public Guid FormId { get; init; }
    public string? UserId { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool IncludeForm { get; init; } = false;
}


public record GetSubmissionStatsQuery : IRequest<Result<SubmissionStatsDto>>
{
    public Guid FormId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}