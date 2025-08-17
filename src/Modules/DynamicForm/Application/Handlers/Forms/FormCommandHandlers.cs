using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Forms;

public class CreateFormCommandHandler : IRequestHandler<CreateFormCommand, Result<FormDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly IFormSchemaValidator _schemaValidator;
    private readonly ILogger<CreateFormCommandHandler> _logger;

    public CreateFormCommandHandler(
        IFormRepository formRepository,
        IFormSchemaValidator schemaValidator,
        ILogger<CreateFormCommandHandler> logger)
    {
        _formRepository = formRepository;
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    public async Task<Result<FormDto>> Handle(CreateFormCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate schema
            var schemaValidation = await _schemaValidator.ValidateAsync(request.SchemaJson, cancellationToken);
            if (!schemaValidation.IsValid)
            {
                return Result<FormDto>.Failure(schemaValidation.Errors.Select(e => e.Message).ToArray());
            }

            // Check if form with same name exists in tenant
            var existingForm = await _formRepository.GetByNameAsync(request.Name, request.TenantId, cancellationToken);
            if (existingForm != null)
            {
                return Result<FormDto>.Failure($"Form with name '{request.Name}' already exists in this tenant.");
            }

            // Create form entity
            var form = new Form
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SchemaJson = request.SchemaJson,
                IsActive = request.IsActive,
                TenantId = request.TenantId,
                BusinessId = request.BusinessId,
                Metadata = request.Metadata,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            await _formRepository.AddAsync(form, cancellationToken);
            await _formRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Form created successfully with ID: {FormId}", form.Id);

            // Map to DTO
            var formDto = MapToDto(form);
            return Result<FormDto>.Success(formDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form: {FormName}", request.Name);
            return Result<FormDto>.Failure($"Error creating form: {ex.Message}");
        }
    }

    private static FormDto MapToDto(Form form)
    {
        return new FormDto
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
    }
}

public class UpdateFormCommandHandler : IRequestHandler<UpdateFormCommand, Result<FormDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly IFormSchemaValidator _schemaValidator;
    private readonly ILogger<UpdateFormCommandHandler> _logger;

    public UpdateFormCommandHandler(
        IFormRepository formRepository,
        IFormSchemaValidator schemaValidator,
        ILogger<UpdateFormCommandHandler> logger)
    {
        _formRepository = formRepository;
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    public async Task<Result<FormDto>> Handle(UpdateFormCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByIdAsync(request.Id, cancellationToken);
            if (form == null)
            {
                return Result<FormDto>.Failure($"Form with ID {request.Id} not found.");
            }

            // Validate schema
            var schemaValidation = await _schemaValidator.ValidateAsync(request.SchemaJson, cancellationToken);
            if (!schemaValidation.IsValid)
            {
                return Result<FormDto>.Failure(schemaValidation.Errors.Select(e => e.Message).ToArray());
            }

            // Update form properties
            form.Name = request.Name;
            form.Description = request.Description;
            form.SchemaJson = request.SchemaJson;
            form.IsActive = request.IsActive;
            form.Metadata = request.Metadata;
            form.UpdatedAt = DateTime.UtcNow;
            form.Version++;

            await _formRepository.UpdateAsync(form, cancellationToken);
            await _formRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Form updated successfully with ID: {FormId}", form.Id);

            var formDto = MapToDto(form);
            return Result<FormDto>.Success(formDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form: {FormId}", request.Id);
            return Result<FormDto>.Failure($"Error updating form: {ex.Message}");
        }
    }

    private static FormDto MapToDto(Form form)
    {
        return new FormDto
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
    }
}

public class DeleteFormCommandHandler : IRequestHandler<DeleteFormCommand, Result<bool>>
{
    private readonly IFormRepository _formRepository;
    private readonly ILogger<DeleteFormCommandHandler> _logger;

    public DeleteFormCommandHandler(
        IFormRepository formRepository,
        ILogger<DeleteFormCommandHandler> logger)
    {
        _formRepository = formRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteFormCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByIdAsync(request.Id, cancellationToken);
            if (form == null)
            {
                return Result<bool>.Failure($"Form with ID {request.Id} not found.");
            }

            await _formRepository.DeleteAsync(form, cancellationToken);
            await _formRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Form deleted successfully with ID: {FormId}", request.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form: {FormId}", request.Id);
            return Result<bool>.Failure($"Error deleting form: {ex.Message}");
        }
    }
}

public class ValidateFormCommandHandler : IRequestHandler<ValidateFormCommand, Result<ValidationResultDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly IValidationEngine _validationEngine;
    private readonly ILogger<ValidateFormCommandHandler> _logger;

    public ValidateFormCommandHandler(
        IFormRepository formRepository,
        IValidationEngine validationEngine,
        ILogger<ValidateFormCommandHandler> logger)
    {
        _formRepository = formRepository;
        _validationEngine = validationEngine;
        _logger = logger;
    }

    public async Task<Result<ValidationResultDto>> Handle(ValidateFormCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByIdWithIncludesAsync(request.FormId, includeFields: true, cancellationToken: cancellationToken);
            if (form == null)
            {
                return Result<ValidationResultDto>.Failure($"Form with ID {request.FormId} not found.");
            }

            // Convert form fields to field definitions for validation
            var fieldDefinitions = form.Fields?.Select(f => new FieldDefinition
            {
                Name = f.Name,
                Type = f.FieldType,
                IsRequired = f.IsRequired,
                ValidationRules = f.ValidationRules?.Select(vr => new ValidationRule
                {
                    Type = vr.Key,
                    Parameters = vr.Value as Dictionary<string, object> ?? new Dictionary<string, object>()
                }).ToList() ?? new List<ValidationRule>(),
                Options = f.Options?.Select(o => new FieldOption
                {
                    Value = o.Key,
                    Label = o.Value?.ToString() ?? o.Key
                }).ToList() ?? new List<FieldOption>()
            }).ToList() ?? new List<FieldDefinition>();

            var validationResult = await _validationEngine.ValidateAsync(
                request.FormData,
                fieldDefinitions,
                request.CultureCode,
                cancellationToken);

            var resultDto = new ValidationResultDto
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors.Select(e => new ValidationErrorDto
                {
                    FieldName = e.FieldName,
                    ErrorCode = e.ErrorCode,
                    Message = e.Message,
                    AttemptedValue = e.AttemptedValue,
                    Context = e.Context
                }).ToList(),
                Warnings = validationResult.Warnings.Select(w => new ValidationWarningDto
                {
                    FieldName = w.FieldName,
                    WarningCode = w.WarningCode,
                    Message = w.Message,
                    AttemptedValue = w.AttemptedValue,
                    Context = w.Context
                }).ToList(),
                FieldResults = validationResult.FieldResults.Select(fr => new FieldValidationResultDto
                {
                    FieldName = fr.FieldName,
                    IsValid = fr.IsValid,
                    Errors = fr.Errors.Select(e => new ValidationErrorDto
                    {
                        FieldName = e.FieldName,
                        ErrorCode = e.ErrorCode,
                        Message = e.Message,
                        AttemptedValue = e.AttemptedValue,
                        Context = e.Context
                    }).ToList(),
                    Warnings = fr.Warnings.Select(w => new ValidationWarningDto
                    {
                        FieldName = w.FieldName,
                        WarningCode = w.WarningCode,
                        Message = w.Message,
                        AttemptedValue = w.AttemptedValue,
                        Context = w.Context
                    }).ToList(),
                    ValidatedValue = fr.ValidatedValue
                }).ToList(),
                Metrics = validationResult.Metrics != null ? new ValidationMetricsDto
                {
                    ValidationDuration = validationResult.Metrics.ValidationDuration,
                    TotalFieldsValidated = validationResult.Metrics.TotalFieldsValidated,
                    FieldsWithErrors = validationResult.Metrics.FieldsWithErrors,
                    FieldsWithWarnings = validationResult.Metrics.FieldsWithWarnings,
                    RulesEvaluated = validationResult.Metrics.RulesEvaluated,
                    AdditionalMetrics = validationResult.Metrics.AdditionalMetrics
                } : null
            };

            _logger.LogInformation("Form validation completed for FormId: {FormId}, IsValid: {IsValid}", request.FormId, validationResult.IsValid);
            return Result<ValidationResultDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form: {FormId}", request.FormId);
            return Result<ValidationResultDto>.Failure($"Error validating form: {ex.Message}");
        }
    }
}