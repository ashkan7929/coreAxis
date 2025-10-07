using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.Queries.Forms;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers
{
    /// <summary>
    /// Controller for managing form events.
    /// </summary>
    [ApiController]
    [Route("api/dynamic-forms/events")]
    [Authorize]
    [RequirePermission("Forms", "manage_access")]
    public class FormEventsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FormEventsController> _logger;

        public FormEventsController(
            IMediator mediator,
            ILogger<FormEventsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Trigger a form event.
        /// </summary>
        /// <param name="request">The event trigger request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The event execution results.</returns>
        [HttpPost("trigger")]
        [ProducesResponseType(typeof(List<FormEventResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TriggerEvent(
            [FromBody] TriggerFormEventRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var command = new TriggerFormEventCommand
                {
                    FormId = request.FormId,
                    EventType = request.EventType,
                    UserId = User.Identity?.Name ?? "Unknown",
                    TenantId = request.TenantId,
                    FormData = request.FormData,
                    PreviousFormData = request.PreviousFormData,
                    ChangedField = request.ChangedField,
                    Metadata = request.Metadata
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                _logger.LogWarning("Failed to trigger form event: {Error}", result.Error);
                return BadRequest(result.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering form event");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while triggering the form event.");
            }
        }

        /// <summary>
        /// Get form event handlers information.
        /// </summary>
        /// <param name="formId">The form ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The form event handlers information.</returns>
        [HttpGet("{formId:guid}/handlers")]
        [ProducesResponseType(typeof(FormEventHandlersDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHandlers(
            [FromRoute] Guid formId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetFormEventHandlersQuery { FormId = formId };
                var result = await _mediator.Send(query, cancellationToken);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                _logger.LogWarning("Failed to get form event handlers: {Error}", result.Error);
                return BadRequest(result.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form event handlers for form {FormId}", formId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting form event handlers.");
            }
        }
    }

    /// <summary>
    /// Request model for triggering form events.
    /// </summary>
    public class TriggerFormEventRequest
    {
        /// <summary>
        /// Gets or sets the form ID.
        /// </summary>
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public FormEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current form data.
        /// </summary>
        public Dictionary<string, object?> FormData { get; set; } = new();

        /// <summary>
        /// Gets or sets the previous form data (for onChange events).
        /// </summary>
        public Dictionary<string, object?> PreviousFormData { get; set; } = new();

        /// <summary>
        /// Gets or sets the changed field name (for onChange events).
        /// </summary>
        public string? ChangedField { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the event.
        /// </summary>
        public Dictionary<string, object?> Metadata { get; set; } = new();
    }
}