using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Queries.FormSteps;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.BuildingBlocks.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.FormSteps
{
    /// <summary>
    /// Query handlers for form step operations.
    /// </summary>
    public class FormStepQueryHandlers :
        IRequestHandler<GetFormStepByIdQuery, FormStepDto>,
        IRequestHandler<GetFormStepsByFormIdQuery, IEnumerable<FormStepDto>>
    {
        private readonly IFormStepRepository _formStepRepository;
        private readonly ILogger<FormStepQueryHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepQueryHandlers"/> class.
        /// </summary>
        /// <param name="formStepRepository">The form step repository.</param>
        /// <param name="logger">The logger.</param>
        public FormStepQueryHandlers(
            IFormStepRepository formStepRepository,
            ILogger<FormStepQueryHandlers> logger)
        {
            _formStepRepository = formStepRepository ?? throw new ArgumentNullException(nameof(formStepRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the get form step by ID query.
        /// </summary>
        /// <param name="request">The get form step by ID query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step DTO.</returns>
        public async Task<FormStepDto> Handle(GetFormStepByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Retrieving form step {StepId}", request.Id);

                var formStep = await _formStepRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                
                if (formStep == null)
                {
                    throw new NotFoundException($"Form step with ID {request.Id} not found");
                }

                if (!request.IncludeInactive && !formStep.IsActive)
                {
                    throw new NotFoundException($"Form step with ID {request.Id} is inactive");
                }

                _logger.LogInformation("Form step {StepId} retrieved successfully", request.Id);

                return MapToDto(formStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form step {StepId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Handles the get form steps by form ID query.
        /// </summary>
        /// <param name="request">The get form steps by form ID query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step DTOs.</returns>
        public async Task<IEnumerable<FormStepDto>> Handle(GetFormStepsByFormIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Retrieving form steps for form {FormId}", request.FormId);

                IEnumerable<FormStep> formSteps;

                if (request.RequiredOnly)
                {
                    formSteps = await _formStepRepository.GetRequiredStepsAsync(request.FormId, request.TenantId, cancellationToken);
                }
                else if (request.SkippableOnly)
                {
                    formSteps = await _formStepRepository.GetSkippableStepsAsync(request.FormId, request.TenantId, cancellationToken);
                }
                else
                {
                    formSteps = await _formStepRepository.GetByFormIdAsync(request.FormId, request.TenantId, cancellationToken);
                }

                // Apply filters
                if (!request.IncludeInactive)
                {
                    formSteps = formSteps.Where(s => s.IsActive);
                }

                // Apply ordering
                if (request.OrderByStepNumber)
                {
                    formSteps = formSteps.OrderBy(s => s.StepNumber);
                }

                var result = formSteps.Select(MapToDto).ToList();

                _logger.LogInformation("Retrieved {Count} form steps for form {FormId}", result.Count, request.FormId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form steps for form {FormId}", request.FormId);
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