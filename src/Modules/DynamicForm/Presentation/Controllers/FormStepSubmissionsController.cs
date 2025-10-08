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
using CoreAxis.SharedKernel.Exceptions;
using NotFoundException = CoreAxis.SharedKernel.Exceptions.EntityNotFoundException;

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
        /// <remarks>
        /// Functionality: Retrieves a specific form step submission, optionally including step metadata.
        ///
        /// Sample response (200):
        /// ```json
        /// {
        ///   "id": "f1ed6f9b-1b2a-4d62-a3f4-8cde1234abcd",
        ///   "formSubmissionId": "22222222-2222-2222-2222-222222222222",
        ///   "stepId": "c2ad1b7e-55ef-4c5e-9a61-2c95c1f2b123",
        ///   "status": "Completed",
        ///   "submittedAt": "2025-01-08T10:00:00Z"
        /// }
        /// ```
        ///
        /// Errors:
        /// - 404 Not Found: Submission not found.
        /// - 500 Internal Server Error: Unexpected server error.
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Lists step submissions for a form submission with optional filters.
        ///
        /// Query parameters:
        /// - `formSubmissionId` (required)
        /// - `tenantId` (required)
        /// - `includeFormStep`
        /// - `statusFilter`
        /// - `orderByStepNumber`
        ///
        /// Sample response (200):
        /// ```json
        /// [ { "id": "...", "status": "InProgress" } ]
        /// ```
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Returns analytics including timings and validation errors per step.
        ///
        /// Sample response (200):
        /// ```json
        /// {
        ///   "averageTimeSeconds": 45,
        ///   "validationErrors": [ { "field": "email", "count": 3 } ]
        /// }
        /// ```
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Creates a submission for a specific step within a form submission.
        ///
        /// Sample request:
        /// ```json
        /// {
        ///   "formSubmissionId": "22222222-2222-2222-2222-222222222222",
        ///   "stepId": "c2ad1b7e-55ef-4c5e-9a61-2c95c1f2b123",
        ///   "data": { "attachmentId": "file-123" }
        /// }
        /// ```
        ///
        /// Responses:
        /// - 201 Created
        /// - 400 Bad Request
        /// - 500 Internal Server Error
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Updates a step submission's data or status.
        ///
        /// Sample request:
        /// ```json
        /// {
        ///   "data": { "attachmentId": "file-456" },
        ///   "status": "Completed"
        /// }
        /// ```
        ///
        /// Responses:
        /// - 200 OK
        /// - 400 Bad Request
        /// - 404 Not Found
        /// - 500 Internal Server Error
        /// </remarks>
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
        /// <remarks>
        /// Functionality: Marks a step submission as completed and triggers any completion logic.
        ///
        /// Sample request:
        /// ```json
        /// {
        ///   "status": "Completed",
        ///   "completedBy": "user-123"
        /// }
        /// ```
        ///
        /// Responses:
        /// - 200 OK
        /// - 400 Bad Request
        /// - 404 Not Found
        /// - 500 Internal Server Error
        /// </remarks>
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