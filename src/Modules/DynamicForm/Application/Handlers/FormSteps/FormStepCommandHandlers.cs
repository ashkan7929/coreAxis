using CoreAxis.SharedKernel;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Commands.FormSteps;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.SharedKernel.Exceptions;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FormStepCommandHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepCommandHandlers"/> class.
        /// </summary>
        /// <param name="formStepRepository">The form step repository.</param>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="logger">The logger.</param>
        public FormStepCommandHandlers(
            IFormStepRepository formStepRepository,
            IUnitOfWork unitOfWork,
            ILogger<FormStepCommandHandlers> logger)
        {
            _formStepRepository = formStepRepository ?? throw new ArgumentNullException(nameof(formStepRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
                    request.FormId, request.StepNumber, cancellationToken);
                
                if (existingStep != null)
                {
                    throw new BusinessRuleViolationException(
                        $"Step number {request.StepNumber} already exists for form {request.FormId}");
                }

                // Create new form step entity using public constructor
                var formStep = new FormStep(
                    request.FormId,
                    request.StepNumber,
                    request.Title,
                    request.StepSchema,
                    request.TenantId,
                    request.CreatedBy);

                // Set optional properties
                formStep.Description = request.Description;
                formStep.ValidationRules = request.ValidationRules;
                formStep.ConditionalLogic = request.ConditionalLogic;
                formStep.IsRequired = request.IsRequired;
                formStep.CanSkip = request.CanSkip;
                formStep.MinTimeSeconds = request.MinTimeSeconds;
                formStep.MaxTimeSeconds = request.MaxTimeSeconds;
                formStep.Metadata = request.Metadata;

                // Save to repository
                await _formStepRepository.AddAsync(formStep);
                await _unitOfWork.SaveChangesAsync();

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

                var formStep = await _formStepRepository.GetByIdAsync(request.Id);
                if (formStep == null)
                {
                    throw new EntityNotFoundException(nameof(FormStep), request.Id);
                }

                // Update properties
                if (!string.IsNullOrEmpty(request.Title)) formStep.Title = request.Title;
                if (!string.IsNullOrEmpty(request.Description)) formStep.Description = request.Description;
                if (!string.IsNullOrEmpty(request.StepSchema)) formStep.StepSchema = request.StepSchema;
                if (!string.IsNullOrEmpty(request.ValidationRules)) formStep.ValidationRules = request.ValidationRules;
                if (!string.IsNullOrEmpty(request.ConditionalLogic)) formStep.ConditionalLogic = request.ConditionalLogic;
                if (request.IsRequired.HasValue) formStep.IsRequired = request.IsRequired.Value;
                if (request.CanSkip.HasValue) formStep.CanSkip = request.CanSkip.Value;
                if (request.MinTimeSeconds.HasValue) formStep.MinTimeSeconds = request.MinTimeSeconds.Value;
                if (request.MaxTimeSeconds.HasValue) formStep.MaxTimeSeconds = request.MaxTimeSeconds.Value;
                if (!string.IsNullOrEmpty(request.Metadata)) formStep.Metadata = request.Metadata;
                if (request.IsActive.HasValue) formStep.IsActive = request.IsActive.Value;
                
                formStep.LastModifiedBy = request.LastModifiedBy;
                formStep.LastModifiedOn = DateTime.UtcNow;

                _formStepRepository.Update(formStep);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Form step {StepId} updated successfully", formStep.Id);

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
        /// <returns>True if deleted successfully.</returns>
        public async Task<bool> Handle(DeleteFormStepCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting form step {StepId}", request.Id);

                var formStep = await _formStepRepository.GetByIdAsync(request.Id);
                if (formStep == null)
                {
                    throw new EntityNotFoundException(nameof(FormStep), request.Id);
                }

                if (request.HardDelete)
                {
                    _formStepRepository.Delete(formStep);
                }
                else
                {
                    formStep.IsActive = false;
                    formStep.LastModifiedBy = request.DeletedBy;
                    formStep.LastModifiedOn = DateTime.UtcNow;
                    _formStepRepository.Update(formStep);
                }

                await _unitOfWork.SaveChangesAsync();

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