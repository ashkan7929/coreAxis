using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.Forms;

public record GetFormByIdQuery : IRequest<Result<FormDto>>
{
    public Guid Id { get; init; }
    public bool IncludeFields { get; init; } = true;
    public bool IncludeSubmissions { get; init; } = false;
}

public record GetFormByNameQuery : IRequest<Result<FormDto>>
{
    public string Name { get; init; } = string.Empty;
    public string? TenantId { get; init; }
    public bool IncludeFields { get; init; } = true;
}

public record GetFormsQuery : IRequest<Result<IEnumerable<FormDto>>>
{
    public string? TenantId { get; init; }
    public string? BusinessId { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public bool IncludeFields { get; init; } = false;
}

public record GetFormSchemaQuery : IRequest<Result<FormSchemaDto>>
{
    public Guid FormId { get; init; }
    public bool IncludeValidationRules { get; init; } = true;
    public bool IncludeDependencies { get; init; } = true;
}