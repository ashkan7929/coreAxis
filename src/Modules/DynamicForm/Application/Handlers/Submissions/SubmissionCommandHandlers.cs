using CoreAxis.Modules.DynamicForm.Application.Commands.Submissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Submissions;

public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, Result<FormSubmissionDto>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly IFormRepository _formRepository;
    private readonly IValidationEngine _validationEngine;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateSubmissionCommandHandler> _logger;

    public CreateSubmissionCommandHandler(
        IFormSubmissionRepository submissionRepository,
        IFormRepository formRepository,
        IValidationEngine validationEngine,
        IEventBus eventBus,
        ILogger<CreateSubmissionCommandHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _formRepository = formRepository;
        _validationEngine = validationEngine;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<FormSubmissionDto>> Handle(CreateSubmissionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get form with fields for validation
            var form = await _formRepository.GetByIdWithIncludesAsync(
                request.FormId,
                includeFields: true,
                cancellationToken: cancellationToken);

            if (form == null)
            {
                return Result<FormSubmissionDto>.Failure($"Form with ID {request.FormId} not found.");
            }

            if (!form.IsActive)
            {
                return Result<FormSubmissionDto>.Failure("Form is not active and cannot accept submissions.");
            }

            // Validate submission data if requested
            if (request.ValidateBeforeSubmit)
            {
                var fieldDefinitions = form.Fields?.Select(f => {
                    var validationRules = !string.IsNullOrEmpty(f.ValidationRules)
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(f.ValidationRules)
                        : new Dictionary<string, object>();
                    
                    var options = !string.IsNullOrEmpty(f.Options)
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(f.Options)
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
                    request.SubmissionData.ToDictionary(k => k.Key, v => (object?)v.Value),
                    fieldDefinitions,
                    null); // CultureCode missing in command

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.FormErrors.Select(e => $"{e.FieldName}: {e.Message}").ToArray();
                    return Result<FormSubmissionDto>.Failure(errors);
                }
            }

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return Result<FormSubmissionDto>.Failure("Invalid User ID.");
            }

            // Create submission entity
            var submission = new FormSubmission(
                request.FormId,
                userId,
                "default", // TenantId placeholder
                System.Text.Json.JsonSerializer.Serialize(request.SubmissionData),
                request.IpAddress,
                request.UserAgent
            )
            {
                SessionId = request.SessionId,
                Metadata = request.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) : null
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
            await _submissionRepository.SaveChangesAsync(cancellationToken);

            // Publish integration event
            await _eventBus.PublishAsync(new FormSubmitted(
                submission.FormId,
                submission.Id,
                submission.UserId,
                submission.Data,
                submission.Metadata,
                Guid.NewGuid()
            ));

            _logger.LogInformation("Form submission created successfully with ID: {SubmissionId} for Form: {FormId}", submission.Id, request.FormId);

            // Map to DTO
            var submissionDto = MapToDto(submission);
            return Result<FormSubmissionDto>.Success(submissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form submission for FormId: {FormId}", request.FormId);
            return Result<FormSubmissionDto>.Failure($"Error creating form submission: {ex.Message}");
        }
    }

    private static FormSubmissionDto MapToDto(FormSubmission submission)
    {
        return new FormSubmissionDto
        {
            Id = submission.Id,
            FormId = submission.FormId,
            SubmissionData = !string.IsNullOrEmpty(submission.Data) ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(submission.Data) : new Dictionary<string, object>(),
            UserId = submission.UserId.ToString(),
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = !string.IsNullOrEmpty(submission.Metadata) ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(submission.Metadata) : null,
            SubmittedAt = submission.SubmittedAt ?? submission.CreatedOn, // Fallback
            UpdatedAt = submission.LastModifiedOn
        };
    }
}

public class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand, Result<FormSubmissionDto>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly IFormRepository _formRepository;
    private readonly IValidationEngine _validationEngine;
    private readonly ILogger<UpdateSubmissionCommandHandler> _logger;

    public UpdateSubmissionCommandHandler(
        IFormSubmissionRepository submissionRepository,
        IFormRepository formRepository,
        IValidationEngine validationEngine,
        ILogger<UpdateSubmissionCommandHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _formRepository = formRepository;
        _validationEngine = validationEngine;
        _logger = logger;
    }

    public async Task<Result<FormSubmissionDto>> Handle(UpdateSubmissionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var submission = await _submissionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (submission == null)
            {
                return Result<FormSubmissionDto>.Failure($"Submission with ID {request.Id} not found.");
            }

            // Validate submission data if requested
            if (request.ValidateBeforeUpdate)
            {
                var form = await _formRepository.GetByIdWithIncludesAsync(
                    submission.FormId,
                    includeFields: true,
                    cancellationToken: cancellationToken);

                if (form != null)
                {
                    var fieldDefinitions = form.Fields?.Select(f => {
                        var validationRules = !string.IsNullOrEmpty(f.ValidationRules)
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(f.ValidationRules)
                            : new Dictionary<string, object>();
                        
                        var options = !string.IsNullOrEmpty(f.Options)
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(f.Options)
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
                        request.SubmissionData.ToDictionary(k => k.Key, v => (object?)v.Value),
                        fieldDefinitions,
                        null);

                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.FormErrors.Select(e => $"{e.FieldName}: {e.Message}").ToArray();
                        return Result<FormSubmissionDto>.Failure(errors);
                    }
                }
            }

            // Update submission
            submission.Data = System.Text.Json.JsonSerializer.Serialize(request.SubmissionData);
            submission.Metadata = request.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) : null;
            submission.Status = request.Status;
            submission.LastModifiedOn = DateTime.UtcNow;

            await _submissionRepository.UpdateAsync(submission, cancellationToken);
            await _submissionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Form submission updated successfully with ID: {SubmissionId}", submission.Id);

            var submissionDto = MapToDto(submission);
            return Result<FormSubmissionDto>.Success(submissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form submission: {SubmissionId}", request.Id);
            return Result<FormSubmissionDto>.Failure($"Error updating form submission: {ex.Message}");
        }
    }

    private static FormSubmissionDto MapToDto(FormSubmission submission)
    {
        return new FormSubmissionDto
        {
            Id = submission.Id,
            FormId = submission.FormId,
            SubmissionData = !string.IsNullOrEmpty(submission.Data) ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(submission.Data) : new Dictionary<string, object>(),
            UserId = submission.UserId.ToString(),
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = !string.IsNullOrEmpty(submission.Metadata) ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(submission.Metadata) : null,
            SubmittedAt = submission.SubmittedAt ?? submission.CreatedOn,
            UpdatedAt = submission.LastModifiedOn
        };
    }
}

public class DeleteSubmissionCommandHandler : IRequestHandler<DeleteSubmissionCommand, Result<bool>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly ILogger<DeleteSubmissionCommandHandler> _logger;

    public DeleteSubmissionCommandHandler(
        IFormSubmissionRepository submissionRepository,
        ILogger<DeleteSubmissionCommandHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteSubmissionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var submission = await _submissionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (submission == null)
            {
                return Result<bool>.Failure($"Submission with ID {request.Id} not found.");
            }

            await _submissionRepository.RemoveAsync(submission, cancellationToken);
            await _submissionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Form submission deleted successfully with ID: {SubmissionId}", request.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form submission: {SubmissionId}", request.Id);
            return Result<bool>.Failure($"Error deleting form submission: {ex.Message}");
        }
    }
}

public class ValidateSubmissionCommandHandler : IRequestHandler<ValidateSubmissionCommand, Result<ValidationResultDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly IValidationEngine _validationEngine;
    private readonly ILogger<ValidateSubmissionCommandHandler> _logger;

    public ValidateSubmissionCommandHandler(
        IFormRepository formRepository,
        IValidationEngine validationEngine,
        ILogger<ValidateSubmissionCommandHandler> logger)
    {
        _formRepository = formRepository;
        _validationEngine = validationEngine;
        _logger = logger;
    }

    public async Task<Result<ValidationResultDto>> Handle(ValidateSubmissionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _formRepository.GetByIdWithIncludesAsync(
                request.FormId,
                includeFields: true,
                cancellationToken: cancellationToken);

            if (form == null)
            {
                return Result<ValidationResultDto>.Failure($"Form with ID {request.FormId} not found.");
            }

            // Convert form fields to field definitions for validation
            var fieldDefinitions = form.Fields?.Select(f => {
                var validationRules = !string.IsNullOrEmpty(f.ValidationRules)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(f.ValidationRules)
                    : new Dictionary<string, object>();
                
                var options = !string.IsNullOrEmpty(f.Options)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(f.Options)
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
                request.SubmissionData.ToDictionary(k => k.Key, v => (object?)v.Value),
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
                    }).ToList(),
                    ValidatedValue = fr.Value.SanitizedValue
                }).ToList(),
                Metrics = validationResult.Metrics != null ? new ValidationMetricsDto
                {
                    ValidationDuration = validationResult.Metrics.TotalTime,
                    TotalFieldsValidated = validationResult.Metrics.FieldsValidated,
                    FieldsWithErrors = validationResult.FieldResults.Count(fr => !fr.Value.IsValid),
                    FieldsWithWarnings = validationResult.FieldResults.Count(fr => fr.Value.Warnings.Any()),
                    RulesEvaluated = validationResult.Metrics.ExpressionsEvaluated,
                    AdditionalMetrics = new Dictionary<string, object>
                    {
                        { "CustomRulesExecuted", validationResult.Metrics.CustomRulesExecuted },
                        { "StartTime", validationResult.Metrics.StartTime },
                        { "EndTime", validationResult.Metrics.EndTime }
                    }
                } : null
            };

            _logger.LogInformation("Submission validation completed for FormId: {FormId}, IsValid: {IsValid}", request.FormId, validationResult.IsValid);
            return Result<ValidationResultDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating submission for FormId: {FormId}", request.FormId);
            return Result<ValidationResultDto>.Failure($"Error validating submission: {ex.Message}");
        }
    }
}