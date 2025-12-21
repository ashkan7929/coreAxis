using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.Submissions;

public record CreateSubmissionCommand : IRequest<Result<FormSubmissionDto>>
{
    public Guid FormId { get; init; }
    public Dictionary<string, object> SubmissionData { get; init; } = new();
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public bool ValidateBeforeSubmit { get; init; } = true;
}

public record UpdateSubmissionCommand : IRequest<Result<FormSubmissionDto>>
{
    public Guid Id { get; init; }
    public Dictionary<string, object> SubmissionData { get; init; } = new();
    public string? Status { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public bool ValidateBeforeUpdate { get; init; } = true;
}

public record DeleteSubmissionCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
}

public record ValidateSubmissionCommand : IRequest<Result<ValidationResultDto>>
{
    public Guid FormId { get; init; }
    public Dictionary<string, object> SubmissionData { get; init; } = new();
    public bool ValidateAllFields { get; init; } = true;
    public string? CultureCode { get; init; }
}