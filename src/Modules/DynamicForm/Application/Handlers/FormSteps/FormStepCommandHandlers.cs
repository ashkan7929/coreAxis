using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Commands.FormSteps;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.SharedKernel.Exceptions;
using NotFoundException = CoreAxis.SharedKernel.Exceptions.EntityNotFoundException;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.FormSteps
{
    /// <summary>
    /// Command handlers for form step operations.
    /// </summary>
    public class FormStepCommandHandlers :
        IRequestHandler<CreateFormStepCommand, FormStepDto>,
        IRequestHandler<UpdateFormStepCommand, FormStepDto>,
        IRequestHandler<DeleteFormStepCommand, bool>
    {
        private readonly IFormStepRepository _formStepRepository;
        private readonly ILogger<FormStepCommandHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepCommandHandlers"/> class.
        /// </summary>
        /// <param name="formStepRepository">The form step repository.</param>
        /// <param name="logger">The logger.</param>
        public FormStepCommandHandlers(
            IFormStepRepository formStepRepository,
            ILogger<FormStepCommandHandlers> logger)
        {
            _formStepRepository = formStepRepository ?? throw new ArgumentNullException(nameof(formStepRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the create form step command.
        /// </summary>
        /// <param name="request">The create form step command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created form step DTO.</returns>
        public async Task<FormStepDto> Handle(CreateFormStepCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating form step for form {FormId} with step number {StepNumber}", 
                    request.FormId, request.StepNumber);

                // Check if step number already exists for this form
                var existingStep = await _formStepRepository.GetByFormIdAndStepNumberAsync(
                    request.FormId, request.StepNumber, request.TenantId, cancellationToken);
                
                if (existingStep != null)
                {
                    throw new BusinessRuleValidationException(
                        $"Step number {request.StepNumber} already exists for form {request.FormId}");
                }

                // Create new form step entity
                var formStep = FormStep.Create(
                    request.FormId,
                    request.StepNumber,
                    request.Title,
                    request.Description,
                    request.StepSchema,
                    request.ValidationRules,
                    request.ConditionalLogic,
                    request.IsRequired,
                    request.CanSkip,
                    request.MinTimeSeconds,
                    request.MaxTimeSeconds,
                    request.Metadata,
                    request.TenantId,
                    request.CreatedBy);

                // Save to repository
                await _formStepRepository.AddAsync(formStep, cancellationToken);
                await _formStepRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Form step {StepId} created successfully", formStep.Id);

                // Map to DTO
                return MapToDto(formStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form step for form {FormId}", request.FormId);
                throw;
            }
        }

        /// <summary>
        /// Handles the update form step command.
        /// </summary>
        /// <param name="request">The update form step command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated form step DTO.</returns>
        public async Task<FormStepDto> Handle(UpdateFormStepCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating form step {StepId}", request.Id);

                // Get existing form step
                var formStep = await _formStepRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStep == null)
                {
                    throw new NotFoundException($"Form step with ID {request.Id} not found");
                }

                // Update properties
                if (!string.IsNullOrEmpty(request.Title))
                    formStep.UpdateTitle(request.Title);
                
                if (!string.IsNullOrEmpty(request.Description))
                    formStep.UpdateDescription(request.Description);
                
                if (!string.IsNullOrEmpty(request.StepSchema))
                    formStep.UpdateStepSchema(request.StepSchema);
                
                if (!string.IsNullOrEmpty(request.ValidationRules))
                    formStep.UpdateValidationRules(request.ValidationRules);
                
                if (!string.IsNullOrEmpty(request.ConditionalLogic))
                    formStep.UpdateConditionalLogic(request.ConditionalLogic);
                
                if (request.IsRequired.HasValue)
                    formStep.SetRequired(request.IsRequired.Value);
                
                if (request.CanSkip.HasValue)
                    formStep.SetCanSkip(request.CanSkip.Value);
                
                if (request.MinTimeSeconds.HasValue)
                    formStep.SetMinTimeSeconds(request.MinTimeSeconds.Value);
                
                if (request.MaxTimeSeconds.HasValue)
                    formStep.SetMaxTimeSeconds(request.MaxTimeSeconds.Value);
                
                if (!string.IsNullOrEmpty(request.Metadata))
                    formStep.UpdateMetadata(request.Metadata);
                
                if (request.IsActive.HasValue)
                    formStep.SetActive(request.IsActive.Value);

                formStep.SetLastModified(request.LastModifiedBy);

                // Save changes
                await _formStepRepository.UpdateAsync(formStep, cancellationToken);
                await _formStepRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Form step {StepId} updated successfully", request.Id);

                // Map to DTO
                return MapToDto(formStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form step {StepId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Handles the delete form step command.
        /// </summary>
        /// <param name="request">The delete form step command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if deletion was successful.</returns>
        public async Task<bool> Handle(DeleteFormStepCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting form step {StepId} (Hard delete: {HardDelete})", 
                    request.Id, request.HardDelete);

                // Get existing form step
                var formStep = await _formStepRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                if (formStep == null)
                {
                    throw new NotFoundException($"Form step with ID {request.Id} not found");
                }

                if (request.HardDelete)
                {
                    // Hard delete - permanently remove from database
                    await _formStepRepository.DeleteAsync(formStep, cancellationToken);
                }
                else
                {
                    // Soft delete - mark as inactive
                    formStep.SetActive(false);
                    formStep.SetLastModified(request.DeletedBy);
                    await _formStepRepository.UpdateAsync(formStep, cancellationToken);
                }

                await _formStepRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Form step {StepId} deleted successfully", request.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting form step {StepId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Maps a form step entity to a DTO.
        /// </summary>
        /// <param name="formStep">The form step entity.</param>
        /// <returns>The form step DTO.</returns>
        private static FormStepDto MapToDto(FormStep formStep)
        {
            return new FormStepDto
            {
                Id = formStep.Id,
                FormId = formStep.FormId,
                StepNumber = formStep.StepNumber,
                Title = formStep.Title,
                Description = formStep.Description,
                StepSchema = formStep.StepSchema,
                ValidationRules = formStep.ValidationRules,
                ConditionalLogic = formStep.ConditionalLogic,
                IsRequired = formStep.IsRequired,
                CanSkip = formStep.CanSkip,
                MinTimeSeconds = formStep.MinTimeSeconds,
                MaxTimeSeconds = formStep.MaxTimeSeconds,
                Metadata = formStep.Metadata,
                TenantId = formStep.TenantId,
                CreatedOn = formStep.CreatedOn,
                CreatedBy = formStep.CreatedBy,
                LastModifiedOn = formStep.LastModifiedOn,
                LastModifiedBy = formStep.LastModifiedBy,
                IsActive = formStep.IsActive
            };
        }
    }
}