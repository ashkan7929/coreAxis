using CoreAxis.Modules.DynamicForm.Application.Commands.Submissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Submissions;

public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, Result<FormSubmissionDto>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly IFormRepository _formRepository;
    private readonly IValidationEngine _validationEngine;
    private readonly ILogger<CreateSubmissionCommandHandler> _logger;

    public CreateSubmissionCommandHandler(
        IFormSubmissionRepository submissionRepository,
        IFormRepository formRepository,
        IValidationEngine validationEngine,
        ILogger<CreateSubmissionCommandHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _formRepository = formRepository;
        _validationEngine = validationEngine;
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
                    request.SubmissionData,
                    fieldDefinitions,
                    request.CultureCode,
                    cancellationToken);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => $"{e.FieldName}: {e.Message}").ToArray();
                    return Result<FormSubmissionDto>.Failure(errors);
                }
            }

            // Create submission entity
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = request.FormId,
                SubmissionData = request.SubmissionData,
                UserId = request.UserId,
                SessionId = request.SessionId,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                Status = "Submitted",
                Metadata = request.Metadata,
                SubmittedAt = DateTime.UtcNow
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
            await _submissionRepository.SaveChangesAsync(cancellationToken);

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
            SubmissionData = submission.SubmissionData,
            UserId = submission.UserId,
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = submission.Metadata,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt
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
                        request.SubmissionData,
                        fieldDefinitions,
                        request.CultureCode,
                        cancellationToken);

                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors.Select(e => $"{e.FieldName}: {e.Message}").ToArray();
                        return Result<FormSubmissionDto>.Failure(errors);
                    }
                }
            }

            // Update submission properties
            submission.SubmissionData = request.SubmissionData;
            submission.Status = request.Status;
            submission.Metadata = request.Metadata;
            submission.UpdatedAt = DateTime.UtcNow;

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
            SubmissionData = submission.SubmissionData,
            UserId = submission.UserId,
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = submission.Metadata,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt
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

            await _submissionRepository.DeleteAsync(submission, cancellationToken);
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
                request.SubmissionData,
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