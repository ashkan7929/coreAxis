using System.Text.Json;
using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Application.Services;
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
            var schemaValidation = await _schemaValidator.ValidateJsonAsync(request.SchemaJson);
            if (!schemaValidation.IsSuccess)
            {
                return Result<FormDto>.Failure(schemaValidation.Errors.ToArray());
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
                Schema = request.SchemaJson,
                IsActive = request.IsActive,
                TenantId = request.TenantId,
                BusinessId = request.BusinessId,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                CreatedOn = DateTime.UtcNow,
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
            SchemaJson = form.Schema,
            IsActive = form.IsActive,
            TenantId = form.TenantId,
            BusinessId = form.BusinessId,
            Metadata = !string.IsNullOrEmpty(form.Metadata) ? JsonSerializer.Deserialize<Dictionary<string, object>>(form.Metadata) : null,
            CreatedAt = form.CreatedOn,
            UpdatedAt = form.LastModifiedOn,
            CreatedBy = form.CreatedBy,
            UpdatedBy = form.LastModifiedBy,
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
            var schemaValidation = await _schemaValidator.ValidateJsonAsync(request.SchemaJson);
            if (!schemaValidation.IsSuccess)
            {
                return Result<FormDto>.Failure(schemaValidation.Errors.ToArray());
            }

            // Update form properties
            form.Name = request.Name;
            form.Description = request.Description;
            form.Schema = request.SchemaJson;
            form.IsActive = request.IsActive;
            form.Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null;
            form.LastModifiedOn = DateTime.UtcNow;
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
            SchemaJson = form.Schema,
            IsActive = form.IsActive,
            TenantId = form.TenantId,
            BusinessId = form.BusinessId,
            Metadata = !string.IsNullOrEmpty(form.Metadata) ? JsonSerializer.Deserialize<Dictionary<string, object>>(form.Metadata) : null,
            CreatedAt = form.CreatedOn,
            UpdatedAt = form.LastModifiedOn,
            CreatedBy = form.CreatedBy,
            UpdatedBy = form.LastModifiedBy,
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

            await _formRepository.RemoveAsync(form, cancellationToken);
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
            var fieldDefinitions = form.Fields?.Select(f => {
                var validationRules = !string.IsNullOrEmpty(f.ValidationRules) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(f.ValidationRules) 
                    : new Dictionary<string, object>();
                
                var options = !string.IsNullOrEmpty(f.Options)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(f.Options)
                    : new Dictionary<string, object>();

                return new FieldDefinition
                {
                    Name = f.Name,
                    Type = f.FieldType,
                    IsRequired = f.IsRequired,
                    ValidationRules = validationRules?.Select(vr => new ValidationRule
                    {
                        Type = vr.Key,
                        Parameters = vr.Value as Dictionary<string, object> ?? new Dictionary<string, object>()
                    }).ToList() ?? new List<ValidationRule>(),
                    Options = options?.Select(o => new FieldOption
                    {
                        Value = o.Key,
                        Label = o.Value?.ToString() ?? o.Key
                    }).ToList() ?? new List<FieldOption>()
                };
            }).ToList() ?? new List<FieldDefinition>();

            var validationResult = await _validationEngine.ValidateAsync(
                request.FormData.ToDictionary(k => k.Key, v => (object?)v.Value),
                fieldDefinitions,
                !string.IsNullOrEmpty(request.CultureCode) ? new System.Globalization.CultureInfo(request.CultureCode) : null);

            var resultDto = new ValidationResultDto
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.FormErrors.Select(e => new ValidationErrorDto
                {
                    FieldName = e.FieldName ?? string.Empty,
                    ErrorCode = e.Code,
                    Message = e.Message,
                    Context = e.Context.ToDictionary(k => k.Key, v => (object)v.Value!)
                }).ToList(),
                Warnings = new List<ValidationWarningDto>(),
                FieldResults = validationResult.FieldResults.Select(fr => new FieldValidationResultDto
                {
                    FieldName = fr.Key,
                    IsValid = fr.Value.IsValid,
                    Errors = fr.Value.Errors.Select(e => new ValidationErrorDto
                    {
                        FieldName = e.FieldName ?? fr.Key,
                        ErrorCode = e.Code,
                        Message = e.Message,
                        Context = e.Context.ToDictionary(k => k.Key, v => (object)v.Value!)
                    }).ToList(),
                    Warnings = fr.Value.Warnings.Select(w => new ValidationWarningDto
                    {
                        FieldName = w.FieldName ?? fr.Key,
                        WarningCode = w.Code,
                        Message = w.Message
                    }).ToList()
                }).ToList()
            };

            return Result<ValidationResultDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form: {FormId}", request.FormId);
            return Result<ValidationResultDto>.Failure($"Error validating form: {ex.Message}");
        }
    }
}