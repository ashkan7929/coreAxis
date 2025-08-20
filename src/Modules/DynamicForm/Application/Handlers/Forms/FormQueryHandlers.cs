using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Application.Queries.Forms;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Forms;

public class GetFormByIdQueryHandler : IRequestHandler<GetFormByIdQuery, Result<FormDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly ILogger<GetFormByIdQueryHandler> _logger;

    public GetFormByIdQueryHandler(
        IFormRepository formRepository,
        ILogger<GetFormByIdQueryHandler> logger)
    {
        _formRepository = formRepository;
        _logger = logger;
    }

    public async Task<Result<FormDto>> Handle(GetFormByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByIdWithIncludesAsync(
                request.Id,
                includeFields: request.IncludeFields,
                includeSubmissions: request.IncludeSubmissions,
                cancellationToken: cancellationToken);

            if (form == null)
            {
                return Result<FormDto>.Failure($"Form with ID {request.Id} not found.");
            }

            var formDto = MapToDto(form, request.IncludeFields);
        return Result<FormDto>.Success(formDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form: {FormId}", request.Id);
            return Result<FormDto>.Failure($"Error retrieving form: {ex.Message}");
        }
    }

    private static FormDto MapToDto(Form form, bool includeFields = false)
    {
        var dto = new FormDto
        {
            Id = form.Id,
            Name = form.Name,
            Description = form.Description,
            SchemaJson = form.SchemaJson,
            IsActive = form.IsActive,
            TenantId = form.TenantId,
            BusinessId = form.BusinessId,
            Metadata = form.Metadata,
            CreatedAt = form.CreatedAt,
            UpdatedAt = form.UpdatedAt,
            CreatedBy = form.CreatedBy,
            UpdatedBy = form.UpdatedBy,
            Version = form.Version
        };

        if (includeFields && form.Fields != null)
        {
            dto.Fields = form.Fields.Select(f => new FormFieldDto
            {
                Id = f.Id,
                FormId = f.FormId,
                Name = f.Name,
                Label = f.Label,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue,
                Placeholder = f.Placeholder,
                HelpText = f.HelpText,
                Order = f.Order,
                IsActive = f.IsActive,
                ValidationRules = f.ValidationRules,
                ConditionalLogic = f.ConditionalLogic,
                Options = f.Options,
                Metadata = f.Metadata
            }).ToList();
        }

        return dto;
    }
}

public class GetFormByNameQueryHandler : IRequestHandler<GetFormByNameQuery, Result<FormDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly ILogger<GetFormByNameQueryHandler> _logger;

    public GetFormByNameQueryHandler(
        IFormRepository formRepository,
        ILogger<GetFormByNameQueryHandler> logger)
    {
        _formRepository = formRepository;
        _logger = logger;
    }

    public async Task<Result<FormDto>> Handle(GetFormByNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByNameWithIncludesAsync(
                request.Name,
                request.TenantId,
                includeFields: request.IncludeFields,
                cancellationToken: cancellationToken);

            if (form == null)
            {
                return Result<FormDto>.Failure($"Form with name '{request.Name}' not found in tenant {request.TenantId}.");
            }

            var formDto = MapToDto(form, request.IncludeFields);
        return Result<FormDto>.Success(formDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form by name: {FormName}, TenantId: {TenantId}", request.Name, request.TenantId);
            return Result<FormDto>.Failure($"Error retrieving form: {ex.Message}");
        }
    }

    private static FormDto MapToDto(Form form, bool includeFields = false)
    {
        var dto = new FormDto
        {
            Id = form.Id,
            Name = form.Name,
            Description = form.Description,
            SchemaJson = form.SchemaJson,
            IsActive = form.IsActive,
            TenantId = form.TenantId,
            BusinessId = form.BusinessId,
            Metadata = form.Metadata,
            CreatedAt = form.CreatedAt,
            UpdatedAt = form.UpdatedAt,
            CreatedBy = form.CreatedBy,
            UpdatedBy = form.UpdatedBy,
            Version = form.Version
        };

        if (includeFields && form.Fields != null)
        {
            dto.Fields = form.Fields.Select(f => new FormFieldDto
            {
                Id = f.Id,
                FormId = f.FormId,
                Name = f.Name,
                Label = f.Label,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue,
                Placeholder = f.Placeholder,
                HelpText = f.HelpText,
                Order = f.Order,
                IsActive = f.IsActive,
                ValidationRules = f.ValidationRules,
                ConditionalLogic = f.ConditionalLogic,
                Options = f.Options,
                Metadata = f.Metadata
            }).ToList();
        }

        return dto;
    }
}

public class GetFormsQueryHandler : IRequestHandler<GetFormsQuery, Result<PagedResult<FormDto>>>
{
    private readonly IFormRepository _formRepository;
    private readonly ILogger<GetFormsQueryHandler> _logger;

    public GetFormsQueryHandler(
        IFormRepository formRepository,
        ILogger<GetFormsQueryHandler> logger)
    {
        _formRepository = formRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<FormDto>>> Handle(GetFormsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (forms, totalCount) = await _formRepository.GetPagedAsync(
                request.TenantId,
                request.BusinessId,
                request.IsActive,
                request.SearchTerm,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var formDtos = forms.Select(f => new FormDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                SchemaJson = f.SchemaJson,
                IsActive = f.IsActive,
                TenantId = f.TenantId,
                BusinessId = f.BusinessId,
                Metadata = f.Metadata,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                CreatedBy = f.CreatedBy,
                UpdatedBy = f.UpdatedBy,
                Version = f.Version
            }).ToList();

            var pagedResult = new PagedResult<FormDto>
            {
                Items = formDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Result<PagedResult<FormDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forms with filters: TenantId: {TenantId}, BusinessId: {BusinessId}", request.TenantId, request.BusinessId);
            return Result<PagedResult<FormDto>>.Failure($"Error retrieving forms: {ex.Message}");
        }
    }
}

public class GetFormSchemaQueryHandler : IRequestHandler<GetFormSchemaQuery, Result<FormSchemaDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly IDependencyGraph _dependencyGraph;
    private readonly ILogger<GetFormSchemaQueryHandler> _logger;

    public GetFormSchemaQueryHandler(
        IFormRepository formRepository,
        IDependencyGraph dependencyGraph,
        ILogger<GetFormSchemaQueryHandler> logger)
    {
        _formRepository = formRepository;
        _dependencyGraph = dependencyGraph;
        _logger = logger;
    }

    public async Task<Result<FormSchemaDto>> Handle(GetFormSchemaQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByIdWithIncludesAsync(
                request.FormId,
                includeFields: true,
                cancellationToken: cancellationToken);

            if (form == null)
            {
                return Result<FormSchemaDto>.Failure($"Form with ID {request.FormId} not found.");
            }

            // Get dependency graph for the form
            var dependencies = await _dependencyGraphService.GetDependenciesAsync(request.FormId, cancellationToken);

            var schemaDto = new FormSchemaDto
            {
                FormId = form.Id,
                FormName = form.Name,
                SchemaJson = form.SchemaJson,
                Fields = form.Fields?.Select(f => new FormFieldDto
                {
                    Id = f.Id,
                    FormId = f.FormId,
                    Name = f.Name,
                    Label = f.Label,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    DefaultValue = f.DefaultValue,
                    Placeholder = f.Placeholder,
                    HelpText = f.HelpText,
                    Order = f.Order,
                    IsActive = f.IsActive,
                    ValidationRules = f.ValidationRules,
                    ConditionalLogic = f.ConditionalLogic,
                    Options = f.Options,
                    Metadata = f.Metadata
                }).ToList() ?? new List<FormFieldDto>(),
                ValidationRules = ExtractValidationRules(form),
                Dependencies = dependencies?.Select(d => new FieldDependencyDto
                {
                    SourceField = d.SourceField,
                    TargetField = d.TargetField,
                    DependencyType = d.DependencyType,
                    Expression = d.Expression,
                    IsActive = d.IsActive
                }).ToList() ?? new List<FieldDependencyDto>()
            };

            return Result<FormSchemaDto>.Success(schemaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form schema: {FormId}", request.FormId);
            return Result<FormSchemaDto>.Failure($"Error retrieving form schema: {ex.Message}");
        }
    }

    private static List<ValidationRuleDto> ExtractValidationRules(Form form)
    {
        var rules = new List<ValidationRuleDto>();

        if (form.Fields != null)
        {
            foreach (var field in form.Fields)
            {
                if (field.ValidationRules != null)
                {
                    foreach (var rule in field.ValidationRules)
                    {
                        rules.Add(new ValidationRuleDto
                        {
                            FieldName = field.Name,
                            RuleType = rule.Key,
                            Parameters = rule.Value as Dictionary<string, object> ?? new Dictionary<string, object>(),
                            ErrorMessage = GetErrorMessage(rule),
                            IsActive = true
                        });
                    }
                }
            }
        }

        return rules;
    }

    private static string GetErrorMessage(KeyValuePair<string, object> rule)
    {
        if (rule.Value is Dictionary<string, object> parameters && parameters.ContainsKey("errorMessage"))
        {
            return parameters["errorMessage"]?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }
}

// Additional DTOs needed for schema
public class FieldDependencyDto
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string DependencyType { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ValidationRuleDto
{
    public string FieldName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}