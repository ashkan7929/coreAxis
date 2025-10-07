using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Commands.FormSteps;
using CoreAxis.Modules.DynamicForm.Application.Queries.FormSteps;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.SharedKernel.Exceptions;
using NotFoundException = CoreAxis.SharedKernel.Exceptions.EntityNotFoundException;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers
{
    /// <summary>
    /// API controller for managing form steps.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormStepsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FormStepsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepsController"/> class.
        /// </summary>
        /// <param name="mediator">The mediator.</param>
        /// <param name="logger">The logger.</param>
        public FormStepsController(IMediator mediator, ILogger<FormStepsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get form step by ID.
        /// </summary>
        /// <param name="id">Form step ID.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="includeInactive">Include inactive steps.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Form step details.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(FormStepDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFormStepById(
            [FromRoute] Guid id,
            [FromQuery, Required] string tenantId,
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetFormStepByIdQuery
                {
                    Id = id,
                    TenantId = tenantId,
                    IncludeInactive = includeInactive
                };

                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (NotFoundException)
            {
                return NotFound($"Form step with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form step with ID: {StepId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the form step");
            }
        }

        /// <summary>
        /// Get form steps by form ID.
        /// </summary>
        /// <param name="formId">Form ID.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="includeInactive">Include inactive steps.</param>
        /// <param name="requiredOnly">Include only required steps.</param>
        /// <param name="skippableOnly">Include only skippable steps.</param>
        /// <param name="orderByStepNumber">Order by step number.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of form steps.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FormStepDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFormStepsByFormId(
            [FromQuery, Required] Guid formId,
            [FromQuery, Required] string tenantId,
            [FromQuery] bool includeInactive = false,
            [FromQuery] bool requiredOnly = false,
            [FromQuery] bool skippableOnly = false,
            [FromQuery] bool orderByStepNumber = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetFormStepsByFormIdQuery
                {
                    FormId = formId,
                    TenantId = tenantId,
                    IncludeInactive = includeInactive,
                    RequiredOnly = requiredOnly,
                    SkippableOnly = skippableOnly,
                    OrderByStepNumber = orderByStepNumber
                };

                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form steps for form ID: {FormId}", formId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving form steps");
            }
        }

        /// <summary>
        /// Create a new form step.
        /// </summary>
        /// <param name="command">Form step creation data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Created form step.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(FormStepDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateFormStep(
            [FromBody] CreateFormStepCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetFormStepById), new { id = result.Id, tenantId = command.TenantId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form step");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the form step");
            }
        }

        /// <summary>
        /// Update an existing form step.
        /// </summary>
        /// <param name="id">Form step ID.</param>
        /// <param name="command">Form step update data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated form step.</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(FormStepDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateFormStep(
            [FromRoute] Guid id,
            [FromBody] UpdateFormStepCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                command.Id = id;
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (NotFoundException)
            {
                return NotFound($"Form step with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form step with ID: {StepId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the form step");
            }
        }

        /// <summary>
        /// Delete a form step.
        /// </summary>
        /// <param name="id">Form step ID.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="deletedBy">User performing the deletion.</param>
        /// <param name="hardDelete">Perform hard delete (permanent removal).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Deletion result.</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteFormStep(
            [FromRoute] Guid id,
            [FromQuery, Required] string tenantId,
            [FromQuery, Required] string deletedBy,
            [FromQuery] bool hardDelete = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var command = new DeleteFormStepCommand
                {
                    Id = id,
                    TenantId = tenantId,
                    DeletedBy = deletedBy,
                    HardDelete = hardDelete
                };

                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (NotFoundException)
            {
                return NotFound($"Form step with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting form step with ID: {StepId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the form step");
            }
        }
    }
}