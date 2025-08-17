using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.Submissions;

public record GetSubmissionByIdQuery : IRequest<Result<FormSubmissionDto>>
{
    public Guid Id { get; init; }
    public bool IncludeForm { get; init; } = false;
}

public record GetSubmissionsQuery : IRequest<Result<IEnumerable<FormSubmissionDto>>>
{
    public Guid? FormId { get; init; }
    public string? UserId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool IncludeForm { get; init; } = false;
}

public record GetSubmissionsByFormQuery : IRequest<Result<IEnumerable<FormSubmissionDto>>>
{
    public Guid FormId { get; init; }
    public string? UserId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record GetSubmissionStatsQuery : IRequest<Result<SubmissionStatsDto>>
{
    public Guid FormId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}