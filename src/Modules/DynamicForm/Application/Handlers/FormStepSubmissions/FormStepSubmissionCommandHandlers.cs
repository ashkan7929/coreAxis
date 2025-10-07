using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.SharedKernel.Exceptions;
using NotFoundException = CoreAxis.SharedKernel.Exceptions.EntityNotFoundException;
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
        private readonly ILogger<FormStepSubmissionCommandHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepSubmissionCommandHandlers"/> class.
        /// </summary>
        /// <param name="formStepSubmissionRepository">The form step submission repository.</param>
        /// <param name="formStepRepository">The form step repository.</param>
        /// <param name="logger">The logger.</param>
        public FormStepSubmissionCommandHandlers(
            IFormStepSubmissionRepository formStepSubmissionRepository,
            IFormStepRepository formStepRepository,
            ILogger<FormStepSubmissionCommandHandlers> logger)
        {
            _formStepSubmissionRepository = formStepSubmissionRepository ?? throw new ArgumentNullException(nameof(formStepSubmissionRepository));
            _formStepRepository = formStepRepository ?? throw new ArgumentNullException(nameof(formStepRepository));
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
                    throw new NotFoundException($"Form step with ID {request.FormStepId} not found");
                }

                // Check if step submission already exists
                var existingSubmission = await _formStepSubmissionRepository.GetByFormSubmissionIdAndStepNumberAsync(
                    request.FormSubmissionId, request.StepNumber, request.TenantId, cancellationToken);
                
                if (existingSubmission != null)
                {
                    throw new BusinessRuleValidationException(
                        $"Step submission for step number {request.StepNumber} already exists for form submission {request.FormSubmissionId}");
                }

                // Create new form step submission entity
                var formStepSubmission = FormStepSubmission.Create(
                    request.FormSubmissionId,
                    request.FormStepId,
                    request.StepNumber,
                    request.UserId,
                    request.TenantId,
                    request.StepData,
                    request.Metadata,
                    request.CreatedBy);

                // Save to repository
                await _formStepSubmissionRepository.AddAsync(formStepSubmission, cancellationToken);
                await _formStepSubmissionRepository.SaveChangesAsync(cancellationToken);

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

                // Get existing form step submission
                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStepSubmission == null)
                {
                    throw new NotFoundException($"Form step submission with ID {request.Id} not found");
                }

                // Update properties
                if (!string.IsNullOrEmpty(request.StepData))
                    formStepSubmission.UpdateStepData(request.StepData);
                
                if (!string.IsNullOrEmpty(request.Metadata))
                    formStepSubmission.UpdateMetadata(request.Metadata);

                formStepSubmission.SetLastModified(request.LastModifiedBy);

                // Save changes
                await _formStepSubmissionRepository.UpdateAsync(formStepSubmission, cancellationToken);
                await _formStepSubmissionRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Form step submission {SubmissionId} updated successfully", request.Id);

                // Map to DTO
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

                // Get existing form step submission
                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStepSubmission == null)
                {
                    throw new NotFoundException($"Form step submission with ID {request.Id} not found");
                }

                // Update step data if provided
                if (!string.IsNullOrEmpty(request.StepData))
                    formStepSubmission.UpdateStepData(request.StepData);
                
                if (!string.IsNullOrEmpty(request.Metadata))
                    formStepSubmission.UpdateMetadata(request.Metadata);

                // Complete the step submission
                formStepSubmission.Complete();
                formStepSubmission.SetLastModified(request.CompletedBy);

                // Save changes
                await _formStepSubmissionRepository.UpdateAsync(formStepSubmission, cancellationToken);
                await _formStepSubmissionRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Form step submission {SubmissionId} completed successfully", request.Id);

                // Map to DTO
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

                // Get existing form step submission
                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStepSubmission == null)
                {
                    throw new NotFoundException($"Form step submission with ID {request.Id} not found");
                }

                // Update metadata if provided
                if (!string.IsNullOrEmpty(request.Metadata))
                    formStepSubmission.UpdateMetadata(request.Metadata);

                // Skip the step submission
                formStepSubmission.Skip(request.SkipReason);
                formStepSubmission.SetLastModified(request.SkippedBy);

                // Save changes
                await _formStepSubmissionRepository.UpdateAsync(formStepSubmission, cancellationToken);
                await _formStepSubmissionRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Form step submission {SubmissionId} skipped successfully", request.Id);

                // Map to DTO
                return MapToDto(formStepSubmission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error skipping form step submission {SubmissionId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Maps a form step submission entity to a DTO.
        /// </summary>
        /// <param name="formStepSubmission">The form step submission entity.</param>
        /// <returns>The form step submission DTO.</returns>
        private static FormStepSubmissionDto MapToDto(FormStepSubmission formStepSubmission)
        {
            return new FormStepSubmissionDto
            {
                Id = formStepSubmission.Id,
                FormSubmissionId = formStepSubmission.FormSubmissionId,
                FormStepId = formStepSubmission.FormStepId,
                StepNumber = formStepSubmission.StepNumber,
                UserId = formStepSubmission.UserId,
                TenantId = formStepSubmission.TenantId,
                StepData = formStepSubmission.StepData,
                Status = formStepSubmission.Status,
                ValidationErrors = formStepSubmission.ValidationErrors,
                StartedAt = formStepSubmission.StartedAt,
                CompletedAt = formStepSubmission.CompletedAt,
                TimeSpentSeconds = formStepSubmission.TimeSpentSeconds,
                IsSkipped = formStepSubmission.IsSkipped,
                SkipReason = formStepSubmission.SkipReason,
                Metadata = formStepSubmission.Metadata,
                CreatedOn = formStepSubmission.CreatedOn,
                CreatedBy = formStepSubmission.CreatedBy,
                LastModifiedOn = formStepSubmission.LastModifiedOn,
                LastModifiedBy = formStepSubmission.LastModifiedBy,
                IsActive = formStepSubmission.IsActive
            };
        }
    }
}