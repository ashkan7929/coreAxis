using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.Forms;

public record CreateFormCommand : IRequest<Result<FormDto>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public object SchemaJson { get; init; } = new object();
    public bool IsActive { get; init; } = true;
    public string? TenantId { get; init; }
    public string? BusinessId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record UpdateFormCommand : IRequest<Result<FormDto>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public object SchemaJson { get; init; } = new object();
    public bool IsActive { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public record DeleteFormCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
}

public record ValidateFormCommand : IRequest<Result<ValidationResultDto>>
{
    public Guid FormId { get; init; }
    public Dictionary<string, object> FormData { get; init; } = new();
    public bool ValidateAllFields { get; init; } = true;
    public string? CultureCode { get; init; }
}