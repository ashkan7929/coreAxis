using CoreAxis.Modules.DynamicForm.Application.Commands.Submissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Application.Queries.Submissions;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(IMediator mediator, ILogger<SubmissionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get submission by ID
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <param name="includeForm">Include form details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission details</returns>
    /// <remarks>
    /// Example success:
    ///
    /// ```json
    /// {
    ///   "id": "11111111-1111-1111-1111-111111111111",
    ///   "formId": "22222222-2222-2222-2222-222222222222",
    ///   "userId": "33333333-3333-3333-3333-333333333333",
    ///   "status": "Submitted",
    ///   "data": { "fieldA": "value" },
    ///   "createdAt": "2025-01-08T10:00:00Z"
    /// }
    /// ```
    ///
    /// Example not found:
    ///
    /// ```json
    /// {
    ///   "errors": ["Submission not found"]
    /// }
    /// ```
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FormSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubmissionById(
        [FromRoute] Guid id,
        [FromQuery] bool includeForm = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSubmissionByIdQuery
            {
                Id = id,
                IncludeForm = includeForm
            };
            
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return NotFound(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submission with ID: {SubmissionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the submission");
        }
    }

    /// <summary>
    /// Get submissions with filtering and pagination
    /// </summary>
    /// <param name="formId">Form ID filter</param>
    /// <param name="userId">User ID filter</param>
    /// <param name="status">Status filter</param>
    /// <param name="fromDate">From date filter</param>
    /// <param name="toDate">To date filter</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="includeForm">Include form details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of submissions</returns>
    /// <remarks>
    /// Example response:
    ///
    /// ```json
    /// {
    ///   "items": [
    ///     {
    ///       "id": "11111111-1111-1111-1111-111111111111",
    ///       "formId": "22222222-2222-2222-2222-222222222222",
    ///       "status": "Submitted",
    ///       "createdAt": "2025-01-08T10:00:00Z"
    ///     }
    ///   ],
    ///   "totalCount": 1,
    ///   "pageNumber": 1,
    ///   "pageSize": 10,
    ///   "totalPages": 1
    /// }
    /// ```
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FormSubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubmissions(
        [FromQuery] Guid? formId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeForm = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSubmissionsQuery
            {
                FormId = formId,
                UserId = userId?.ToString(),
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize,
                IncludeForm = includeForm
            };
            
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submissions");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving submissions");
        }
    }

    /// <summary>
    /// Create a new submission
    /// </summary>
    /// <param name="command">Submission creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created submission</returns>
    /// <remarks>
    /// Example request:
    ///
    /// ```json
    /// {
    ///   "formId": "22222222-2222-2222-2222-222222222222",
    ///   "data": { "fieldA": "value" }
    /// }
    /// ```
    ///
    /// Example success (201):
    ///
    /// ```json
    /// {
    ///   "id": "11111111-1111-1111-1111-111111111111",
    ///   "formId": "22222222-2222-2222-2222-222222222222",
    ///   "status": "Submitted",
    ///   "createdAt": "2025-01-08T10:00:00Z"
    /// }
    /// ```
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(FormSubmissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSubmission(
        [FromBody] CreateSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Set additional context from HTTP request
            var commandWithContext = command with
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };
            
            var result = await _mediator.Send(commandWithContext, cancellationToken);
            
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetSubmissionById), new { id = result.Data!.Id }, result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating submission");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the submission");
        }
    }

    /// <summary>
    /// Update an existing submission
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <param name="command">Submission update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated submission</returns>
    /// <remarks>
    /// Example request:
    ///
    /// ```json
    /// {
    ///   "data": { "fieldA": "new value" },
    ///   "status": "Updated"
    /// }
    /// ```
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FormSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSubmission(
        [FromRoute] Guid id,
        [FromBody] UpdateSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commandWithId = command with { Id = id };
            var result = await _mediator.Send(commandWithId, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating submission with ID: {SubmissionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the submission");
        }
    }

    /// <summary>
    /// Delete a submission
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Returns `204 NoContent` on success.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSubmission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteSubmissionCommand { Id = id };
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return NoContent();
            }
            
            return NotFound(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting submission with ID: {SubmissionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the submission");
        }
    }

    /// <summary>
    /// Validate submission data
    /// </summary>
    /// <param name="command">Validation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    /// <remarks>
    /// Example success:
    ///
    /// ```json
    /// {
    ///   "isValid": true,
    ///   "errors": []
    /// }
    /// ```
    /// </remarks>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateSubmission(
        [FromBody] ValidateSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating submission for FormId: {FormId}", command.FormId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while validating the submission");
        }
    }

    /// <summary>
    /// Get submission statistics for a specific form
    /// </summary>
    /// <param name="formId">Form ID</param>
    /// <param name="fromDate">From date filter</param>
    /// <param name="toDate">To date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(SubmissionStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubmissionStats(
        [FromQuery] Guid formId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSubmissionStatsQuery
            {
                FormId = formId,
                FromDate = fromDate,
                ToDate = toDate
            };
            
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submission stats for FormId: {FormId}", formId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving submission statistics");
        }
    }

    /// <summary>
    /// Bulk update submission status
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPatch("bulk-status")]
    [ProducesResponseType(typeof(BulkUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkUpdateStatus(
        [FromBody] BulkUpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var successCount = 0;
            var failedIds = new List<Guid>();
            var errors = new List<string>();

            foreach (var submissionId in request.SubmissionIds)
            {
                try
                {
                    var command = new UpdateSubmissionCommand
                    {
                        Id = submissionId,
                        Status = request.NewStatus,
                        ValidateBeforeUpdate = false
                    };

                    var result = await _mediator.Send(command, cancellationToken);
                    
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedIds.Add(submissionId);
                        errors.AddRange(result.Errors);
                    }
                }
                catch (Exception ex)
                {
                    failedIds.Add(submissionId);
                    errors.Add($"Error updating submission {submissionId}: {ex.Message}");
                    _logger.LogError(ex, "Error in bulk update for submission: {SubmissionId}", submissionId);
                }
            }

            var bulkResult = new BulkUpdateResultDto
            {
                TotalRequested = request.SubmissionIds.Count,
                SuccessCount = successCount,
                FailedCount = failedIds.Count,
                FailedIds = failedIds,
                Errors = errors
            };

            return Ok(bulkResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk status update");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during bulk status update");
        }
    }
}

// Request/Response DTOs for bulk operations
public class BulkUpdateStatusRequest
{
    public List<Guid> SubmissionIds { get; set; } = new();
    public string NewStatus { get; set; } = string.Empty;
}

public class BulkUpdateResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<Guid> FailedIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}