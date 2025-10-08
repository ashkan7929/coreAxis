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
        /// <remarks>
        /// Functionality: Retrieves a specific step of a form including metadata and ordering.
        ///
        /// Sample response (200):
        /// ```json
        /// {
        ///   "id": "c2ad1b7e-55ef-4c5e-9a61-2c95c1f2b123",
        ///   "formId": "6c2b6f1d-3c48-4d9c-9e6b-0e79f67d1234",
        ///   "stepNumber": 2,
        ///   "title": "Personal Details",
        ///   "isRequired": true
        /// }
        /// ```
        ///
        /// Errors:
        /// - 404 Not Found: Step not found.
        /// - 500 Internal Server Error: Unexpected server error.
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Returns all steps for a given form with optional filters.
        ///
        /// Query parameters:
        /// - `formId` (required): Target form ID.
        /// - `tenantId` (required): Tenant context.
        /// - `includeInactive`: Include inactive steps.
        /// - `requiredOnly`: Only include required steps.
        /// - `skippableOnly`: Only include skippable steps.
        /// - `orderByStepNumber`: Sort by step number.
        ///
        /// Sample response (200):
        /// ```json
        /// [ { "id": "...", "stepNumber": 1, "title": "Start" } ]
        /// ```
        ///
        /// Errors:
        /// - 500 Internal Server Error: Unexpected server error.
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Creates a new step with ordering, requirements, and skippable settings.
        ///
        /// Sample request:
        /// ```json
        /// {
        ///   "formId": "6c2b6f1d-3c48-4d9c-9e6b-0e79f67d1234",
        ///   "stepNumber": 3,
        ///   "title": "Attachments",
        ///   "isRequired": false,
        ///   "isSkippable": true
        /// }
        /// ```
        ///
        /// Responses:
        /// - 201 Created: Returns created step.
        /// - 400 Bad Request: Validation errors.
        /// - 500 Internal Server Error: Unexpected server error.
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Updates an existing step's metadata.
        ///
        /// Sample request:
        /// ```json
        /// {
        ///   "title": "Personal Details (Updated)",
        ///   "isRequired": true
        /// }
        /// ```
        ///
        /// Responses:
        /// - 200 OK: Updated step.
        /// - 400 Bad Request: Validation errors.
        /// - 404 Not Found: Step not found.
        /// - 500 Internal Server Error: Unexpected server error.
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Deletes a step either softly or permanently.
        ///
        /// Query parameters:
        /// - `tenantId` (required): Tenant context.
        /// - `deletedBy` (required): Actor performing deletion.
        /// - `hardDelete` (optional): If true, removes permanently.
        ///
        /// Responses:
        /// - 200 OK: Returns boolean success.
        /// - 404 Not Found: Step not found.
        /// - 500 Internal Server Error: Unexpected server error.
        /// </remarks>
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