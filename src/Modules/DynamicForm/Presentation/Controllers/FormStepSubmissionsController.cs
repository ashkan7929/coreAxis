using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions;
using CoreAxis.Modules.DynamicForm.Application.Queries.FormStepSubmissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.BuildingBlocks.SharedKernel.Exceptions;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers
{
    /// <summary>
    /// API controller for managing form step submissions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormStepSubmissionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FormStepSubmissionsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepSubmissionsController"/> class.
        /// </summary>
        /// <param name="mediator">The mediator.</param>
        /// <param name="logger">The logger.</param>
        public FormStepSubmissionsController(IMediator mediator, ILogger<FormStepSubmissionsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get form step submission by ID.
        /// </summary>
        /// <param name="id">Form step submission ID.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="includeFormStep">Include form step details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Form step submission details.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(FormStepSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFormStepSubmissionById(
            [FromRoute] Guid id,
            [FromQuery, Required] string tenantId,
            [FromQuery] bool includeFormStep = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetFormStepSubmissionByIdQuery
                {
                    Id = id,
                    TenantId = tenantId,
                    IncludeFormStep = includeFormStep
                };

                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (NotFoundException)
            {
                return NotFound($"Form step submission with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form step submission with ID: {SubmissionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the form step submission");
            }
        }

        /// <summary>
        /// Get form step submissions by form submission ID.
        /// </summary>
        /// <param name="formSubmissionId">Form submission ID.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="includeFormStep">Include form step details.</param>
        /// <param name="statusFilter">Filter by submission status.</param>
        /// <param name="orderByStepNumber">Order by step number.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of form step submissions.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FormStepSubmissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFormStepSubmissionsByFormSubmissionId(
            [FromQuery, Required] Guid formSubmissionId,
            [FromQuery, Required] string tenantId,
            [FromQuery] bool includeFormStep = false,
            [FromQuery] string statusFilter = null,
            [FromQuery] bool orderByStepNumber = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetFormStepSubmissionsByFormSubmissionIdQuery
                {
                    FormSubmissionId = formSubmissionId,
                    TenantId = tenantId,
                    IncludeFormStep = includeFormStep,
                    StatusFilter = statusFilter,
                    OrderByStepNumber = orderByStepNumber
                };

                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form step submissions for form submission ID: {FormSubmissionId}", formSubmissionId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving form step submissions");
            }
        }

        /// <summary>
        /// Get form step submission analytics.
        /// </summary>
        /// <param name="formSubmissionId">Form submission ID.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="includeTimings">Include timing analytics.</param>
        /// <param name="includeValidationErrors">Include validation error analytics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Form step submission analytics.</returns>
        [HttpGet("analytics")]
        [ProducesResponseType(typeof(FormStepSubmissionAnalyticsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFormStepSubmissionAnalytics(
            [FromQuery, Required] Guid formSubmissionId,
            [FromQuery, Required] string tenantId,
            [FromQuery] bool includeTimings = true,
            [FromQuery] bool includeValidationErrors = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetFormStepSubmissionAnalyticsQuery
                {
                    FormSubmissionId = formSubmissionId,
                    TenantId = tenantId,
                    IncludeTimings = includeTimings,
                    IncludeValidationErrors = includeValidationErrors
                };

                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving form step submission analytics for form submission ID: {FormSubmissionId}", formSubmissionId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving analytics");
            }
        }

        /// <summary>
        /// Create a new form step submission.
        /// </summary>
        /// <param name="command">Form step submission creation data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Created form step submission.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(FormStepSubmissionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateFormStepSubmission(
            [FromBody] CreateFormStepSubmissionCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetFormStepSubmissionById), new { id = result.Id, tenantId = command.TenantId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form step submission");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the form step submission");
            }
        }

        /// <summary>
        /// Update an existing form step submission.
        /// </summary>
        /// <param name="id">Form step submission ID.</param>
        /// <param name="command">Form step submission update data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated form step submission.</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(FormStepSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateFormStepSubmission(
            [FromRoute] Guid id,
            [FromBody] UpdateFormStepSubmissionCommand command,
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
                return NotFound($"Form step submission with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form step submission with ID: {SubmissionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the form step submission");
            }
        }

        /// <summary>
        /// Complete a form step submission.
        /// </summary>
        /// <param name="id">Form step submission ID.</param>
        /// <param name="command">Form step submission completion data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed form step submission.</returns>
        [HttpPost("{id:guid}/complete")]
        [ProducesResponseType(typeof(FormStepSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteFormStepSubmission(
            [FromRoute] Guid id,
            [FromBody] CompleteFormStepSubmissionCommand command,
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
                return NotFound($"Form step submission with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing form step submission with ID: {SubmissionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while completing the form step submission");
            }
        }

        /// <summary>
        /// Skip a form step submission.
        /// </summary>
        /// <param name="id">Form step submission ID.</param>
        /// <param name="command">Form step submission skip data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Skipped form step submission.</returns>
        [HttpPost("{id:guid}/skip")]
        [ProducesResponseType(typeof(FormStepSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SkipFormStepSubmission(
            [FromRoute] Guid id,
            [FromBody] SkipFormStepSubmissionCommand command,
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
                return NotFound($"Form step submission with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error skipping form step submission with ID: {SubmissionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while skipping the form step submission");
            }
        }
    }
}