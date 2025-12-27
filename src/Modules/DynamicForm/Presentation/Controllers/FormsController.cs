using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.Commands.Submissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Application.Queries.Forms;
using CoreAxis.Modules.DynamicForm.Application.Queries.Submissions;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("forms")]
// [Authorize]
// [RequirePermission("Forms", "manage_access")]
[AllowAnonymous]
public class FormsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FormsController> _logger;

    public FormsController(IMediator mediator, ILogger<FormsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new form
    /// </summary>
    /// <param name="command">Form creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created form</returns>
    /// <remarks>
    /// Functionality: Creates a dynamic form with metadata, fields, and access policies.
    ///
    /// Sample request:
    /// ```json
    /// {
    ///   "name": "insurance-form",
    ///   "tenantId": "b4a4b6f2-9f1b-4c3d-b2a2-9c0b1f42c111",
    ///   "title": "Insurance Application",
    ///   "fields": [ { "name": "fullName", "type": "text", "required": true } ],
    ///   "isActive": true
    /// }
    /// ```
    ///
    /// Responses:
    /// - 201 Created: Returns the created form resource.
    /// - 400 Bad Request: Validation errors for malformed input.
    /// - 500 Internal Server Error: Unexpected server error.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(FormDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetFormById), new { id = result.Data!.Id }, result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the form");
        }
    }

    /// <summary>
    /// Get form by ID
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="includeFields">Include form fields</param>
    /// <param name="includeSubmissions">Include form submissions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Form details</returns>
    /// <remarks>
    /// Functionality: Retrieves a single form by its unique identifier.
    ///
    /// Sample response (200):
    /// ```json
    /// {
    ///   "id": "6c2b6f1d-3c48-4d9c-9e6b-0e79f67d1234",
    ///   "name": "insurance-form",
    ///   "title": "Insurance Application",
    ///   "isActive": true,
    ///   "fields": []
    /// }
    /// ```
    ///
    /// Errors:
    /// - 404 Not Found: Form with given ID not found.
    /// - 500 Internal Server Error: Unexpected server error.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FormDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFormById(
        [FromRoute] Guid id,
        [FromQuery] bool includeFields = false,
        [FromQuery] bool includeSubmissions = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFormByIdQuery
            {
                Id = id,
                IncludeFields = includeFields,
                IncludeSubmissions = includeSubmissions
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
            _logger.LogError(ex, "Error retrieving form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the form");
        }
    }

    /// <summary>
    /// Get form by name
    /// </summary>
    /// <param name="name">Form name</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="includeFields">Include form fields</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Form details</returns>
    /// <remarks>
    /// Functionality: Retrieves a form by its `name` within a specific tenant.
    ///
    /// Sample request: GET /api/forms/by-name/insurance-form?tenantId={tenantId}&includeFields=true
    ///
    /// Errors:
    /// - 404 Not Found: Form with given name not found in tenant.
    /// - 500 Internal Server Error: Unexpected server error.
    /// </remarks>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(FormDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFormByName(
        [FromRoute] string name,
        [FromQuery, Required] Guid tenantId,
        [FromQuery] bool includeFields = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFormByNameQuery
            {
                Name = name,
                TenantId = tenantId.ToString(),
                IncludeFields = includeFields
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
            _logger.LogError(ex, "Error retrieving form by name: {FormName}, TenantId: {TenantId}", name, tenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the form");
        }
    }

    /// <summary>
    /// Get forms with filtering and pagination
    /// </summary>
    /// <param name="tenantId">Tenant ID filter</param>
    /// <param name="businessId">Business ID filter</param>
    /// <param name="isActive">Active status filter</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of forms</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FormDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetForms(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? businessId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFormsQuery
            {
                TenantId = tenantId?.ToString(),
                BusinessId = businessId?.ToString(),
                IsActive = isActive,
                SearchTerm = searchTerm,
                Page = pageNumber,
                PageSize = pageSize
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
            _logger.LogError(ex, "Error retrieving forms");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving forms");
        }
    }

    /// <summary>
    /// Get form schema with validation rules and dependencies
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Form schema</returns>
    /// <remarks>
    /// Functionality: Retrieves the schema of a form including field definitions, dependencies, and validation rules.
    ///
    /// Sample response (200):
    /// ```json
    /// {
    ///   "formId": "6c2b6f1d-3c48-4d9c-9e6b-0e79f67d1234",
    ///   "fields": [ { "name": "fullName", "type": "text" } ],
    ///   "dependencies": [],
    ///   "validationRules": []
    /// }
    /// ```
    ///
    /// Errors:
    /// - 404 Not Found: Form not found.
    /// - 500 Internal Server Error: Unexpected server error.
    /// </remarks>
    [HttpGet("{id:guid}/schema")]
    [ProducesResponseType(typeof(FormSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFormSchema(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetFormSchemaQuery { FormId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return NotFound(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form schema for ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the form schema");
        }
    }

    /// <summary>
    /// Update an existing form
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="command">Form update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated form</returns>
    /// <remarks>
    /// Functionality: Updates form metadata or fields.
    ///
    /// Sample request:
    /// ```json
    /// {
    ///   "title": "Insurance Application v2",
    ///   "isActive": true
    /// }
    /// ```
    ///
    /// Responses:
    /// - 200 OK: Returns the updated form.
    /// - 400 Bad Request: Validation errors.
    /// - 404 Not Found: Form not found.
    /// - 500 Internal Server Error: Unexpected server error.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FormDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateForm(
        [FromRoute] Guid id,
        [FromBody] UpdateFormCommand command,
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
            _logger.LogError(ex, "Error updating form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the form");
        }
    }

    /// <summary>
    /// Delete a form
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteForm(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteFormCommand { Id = id };
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return NoContent();
            }
            
            return NotFound(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the form");
        }
    }

    /// <summary>
    /// Validate form data
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="command">Validation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateForm(
        [FromRoute] Guid id,
        [FromBody] ValidateFormCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commandWithId = command with { FormId = id };
            var result = await _mediator.Send(commandWithId, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while validating the form");
        }
    }

    /// <summary>
    /// Evaluate form logic (visibility, calculations)
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="command">Evaluation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Evaluation result</returns>
    [HttpPost("{id:guid}/evaluate")]
    [ProducesResponseType(typeof(FormEvaluationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EvaluateForm(
        [FromRoute] Guid id,
        [FromBody] EvaluateFormCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            command.FormId = id;
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while evaluating the form");
        }
    }

    /// <summary>
    /// Prefill form fields using external services and mappings
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="command">Prefill configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prefilled data</returns>
    [HttpPost("{id:guid}/prefill")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PrefillForm(
        [FromRoute] Guid id,
        [FromBody] PrefillFormCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            command.FormId = id;
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error prefilling form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while prefilling the form");
        }
    }

    /// <summary>
    /// Submit form data
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="command">Submission data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created submission</returns>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(FormSubmissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitForm(
        [FromRoute] Guid id,
        [FromBody] CreateSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commandWithContext = command with
            {
                FormId = id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };
            
            var result = await _mediator.Send(commandWithContext, cancellationToken);
            
            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    "GetSubmissionById", 
                    "Submissions", 
                    new { id = result.Data!.Id }, 
                    result.Data);
            }
            
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting form with ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while submitting the form");
        }
    }

    /// <summary>
    /// Get form submissions
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="userId">User ID filter</param>
    /// <param name="status">Status filter</param>
    /// <param name="fromDate">From date filter</param>
    /// <param name="toDate">To date filter</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="includeForm">Include form details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of submissions</returns>
    [HttpGet("{id:guid}/submissions")]
    [ProducesResponseType(typeof(PagedResult<FormSubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFormSubmissions(
        [FromRoute] Guid id,
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
            var query = new GetSubmissionsByFormQuery
            {
                FormId = id,
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
            _logger.LogError(ex, "Error retrieving submissions for form ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving form submissions");
        }
    }

    /// <summary>
    /// Get form submission statistics
    /// </summary>
    /// <param name="id">Form ID</param>
    /// <param name="fromDate">From date filter</param>
    /// <param name="toDate">To date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission statistics</returns>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(SubmissionStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFormStats(
        [FromRoute] Guid id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSubmissionStatsQuery
            {
                FormId = id,
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
            _logger.LogError(ex, "Error retrieving stats for form ID: {FormId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving form statistics");
        }
    }
}