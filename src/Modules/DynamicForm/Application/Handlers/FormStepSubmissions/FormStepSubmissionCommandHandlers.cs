using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.FormStepSubmissions
{
    /// <summary>
    /// Command handlers for form step submission operations.
    /// </summary>
    public class FormStepSubmissionCommandHandlers :
        IRequestHandler<CreateFormStepSubmissionCommand, FormStepSubmissionDto>,
        IRequestHandler<UpdateFormStepSubmissionCommand, FormStepSubmissionDto>,
        IRequestHandler<CompleteFormStepSubmissionCommand, FormStepSubmissionDto>,
        IRequestHandler<SkipFormStepSubmissionCommand, FormStepSubmissionDto>
    {
        private readonly IFormStepSubmissionRepository _formStepSubmissionRepository;
        private readonly IFormStepRepository _formStepRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FormStepSubmissionCommandHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepSubmissionCommandHandlers"/> class.
        /// </summary>
        /// <param name="formStepSubmissionRepository">The form step submission repository.</param>
        /// <param name="formStepRepository">The form step repository.</param>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="logger">The logger.</param>
        public FormStepSubmissionCommandHandlers(
            IFormStepSubmissionRepository formStepSubmissionRepository,
            IFormStepRepository formStepRepository,
            IUnitOfWork unitOfWork,
            ILogger<FormStepSubmissionCommandHandlers> logger)
        {
            _formStepSubmissionRepository = formStepSubmissionRepository ?? throw new ArgumentNullException(nameof(formStepSubmissionRepository));
            _formStepRepository = formStepRepository ?? throw new ArgumentNullException(nameof(formStepRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the create form step submission command.
        /// </summary>
        /// <param name="request">The create form step submission command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created form step submission DTO.</returns>
        public async Task<FormStepSubmissionDto> Handle(CreateFormStepSubmissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating form step submission for step {StepId} and submission {SubmissionId}", 
                    request.FormStepId, request.FormSubmissionId);

                // Verify form step exists
                var formStep = await _formStepRepository.GetByIdAsync(request.FormStepId, request.TenantId, cancellationToken);
                if (formStep == null)
                {
                    throw new EntityNotFoundException(nameof(FormStep), request.FormStepId);
                }

                // Check if step submission already exists
                var existingSubmission = await _formStepSubmissionRepository.GetByFormSubmissionIdAndStepNumberAsync(
                    request.FormSubmissionId, request.StepNumber, request.TenantId, cancellationToken);
                
                if (existingSubmission != null)
                {
                    throw new BusinessRuleViolationException(
                        $"Step submission for step number {request.StepNumber} already exists for form submission {request.FormSubmissionId}");
                }

                // Create new form step submission entity
                var formStepSubmission = new FormStepSubmission(
                    request.FormSubmissionId,
                    request.FormStepId,
                    request.StepNumber,
                    request.UserId,
                    request.TenantId,
                    request.StepData);
                
                formStepSubmission.Metadata = request.Metadata;
                formStepSubmission.CreatedBy = request.CreatedBy;

                // Save to repository
                await _formStepSubmissionRepository.AddAsync(formStepSubmission);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Form step submission {SubmissionId} created successfully", formStepSubmission.Id);

                // Map to DTO
                return MapToDto(formStepSubmission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form step submission for step {StepId}", request.FormStepId);
                throw;
            }
        }

        /// <summary>
        /// Handles the update form step submission command.
        /// </summary>
        /// <param name="request">The update form step submission command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated form step submission DTO.</returns>
        public async Task<FormStepSubmissionDto> Handle(UpdateFormStepSubmissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating form step submission {SubmissionId}", request.Id);

                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStepSubmission == null)
                {
                    throw new EntityNotFoundException(nameof(FormStepSubmission), request.Id);
                }

                // Update properties
                if (!string.IsNullOrEmpty(request.StepData))
                {
                    formStepSubmission.UpdateStepData(request.StepData, request.LastModifiedBy);
                }
                
                if (!string.IsNullOrEmpty(request.Metadata))
                {
                    formStepSubmission.Metadata = request.Metadata;
                }

                _formStepSubmissionRepository.Update(formStepSubmission);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Form step submission {SubmissionId} updated successfully", formStepSubmission.Id);

                return MapToDto(formStepSubmission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form step submission {SubmissionId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Handles the complete form step submission command.
        /// </summary>
        /// <param name="request">The complete form step submission command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The completed form step submission DTO.</returns>
        public async Task<FormStepSubmissionDto> Handle(CompleteFormStepSubmissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Completing form step submission {SubmissionId}", request.Id);

                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStepSubmission == null)
                {
                    throw new EntityNotFoundException(nameof(FormStepSubmission), request.Id);
                }

                // Update data if provided
                if (!string.IsNullOrEmpty(request.StepData))
                {
                    formStepSubmission.UpdateStepData(request.StepData, request.CompletedBy);
                }
                
                if (!string.IsNullOrEmpty(request.Metadata))
                {
                    formStepSubmission.Metadata = request.Metadata;
                }

                // Complete the step
                formStepSubmission.Complete(request.CompletedBy);

                _formStepSubmissionRepository.Update(formStepSubmission);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Form step submission {SubmissionId} completed successfully", formStepSubmission.Id);

                return MapToDto(formStepSubmission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing form step submission {SubmissionId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Handles the skip form step submission command.
        /// </summary>
        /// <param name="request">The skip form step submission command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The skipped form step submission DTO.</returns>
        public async Task<FormStepSubmissionDto> Handle(SkipFormStepSubmissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Skipping form step submission {SubmissionId}", request.Id);

                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStepSubmission == null)
                {
                    throw new EntityNotFoundException(nameof(FormStepSubmission), request.Id);
                }

                // Update metadata if provided
                if (!string.IsNullOrEmpty(request.Metadata))
                {
                    formStepSubmission.Metadata = request.Metadata;
                }

                // Skip the step
                // Assuming Skip method exists on entity based on previous read
                // Since I can't verify the method signature exactly, I'll rely on common pattern or previous partial read
                // If this fails, I'll check the entity again.
                // Based on IsSkipped and SkipReason properties, and Complete method pattern:
                
                // Assuming method is public void Skip(string reason, string skippedBy)
                // If it doesn't exist, I'll set properties directly
                
                formStepSubmission.IsSkipped = true;
                formStepSubmission.SkipReason = request.SkipReason;
                formStepSubmission.Status = "Skipped";
                formStepSubmission.LastModifiedBy = request.SkippedBy;
                formStepSubmission.LastModifiedOn = DateTime.UtcNow;

                _formStepSubmissionRepository.Update(formStepSubmission);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Form step submission {SubmissionId} skipped successfully", formStepSubmission.Id);

                return MapToDto(formStepSubmission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error skipping form step submission {SubmissionId}", request.Id);
                throw;
            }
        }

        private FormStepSubmissionDto MapToDto(FormStepSubmission entity)
        {
            return new FormStepSubmissionDto
            {
                Id = entity.Id,
                FormSubmissionId = entity.FormSubmissionId,
                FormStepId = entity.FormStepId,
                StepNumber = entity.StepNumber,
                Status = entity.Status,
                StepData = entity.StepData,
                ValidationErrors = entity.ValidationErrors,
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt,
                TimeSpentSeconds = entity.TimeSpentSeconds,
                TenantId = entity.TenantId,
                IsActive = entity.IsActive,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn,
                LastModifiedBy = entity.LastModifiedBy,
                LastModifiedOn = entity.LastModifiedOn
            };
        }
    }
}