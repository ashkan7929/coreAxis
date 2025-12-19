using CoreAxis.Modules.Workflow.Application.Commands;
using CoreAxis.Modules.Workflow.Application.DTOs;
using CoreAxis.Modules.Workflow.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using CoreAxis.Modules.Workflow.Api.Filters;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[ApiExplorerSettings(GroupName = "workflows-runtime")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(IMediator mediator, ILogger<WorkflowController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public record StartWorkflowRequest(string DefinitionCode, int? Version, JsonElement Context, string? CorrelationId);

    /// <summary>
    /// Start a workflow instance.
    /// </summary>
    /// <remarks>
    /// Starts a new workflow run based on the definition code and optional version.
    /// </remarks>
    [HttpPost("start")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request, CancellationToken cancellationToken)
    {
        var dto = new StartWorkflowDto
        {
            DefinitionCode = request.DefinitionCode,
            VersionNumber = request.Version,
            ContextJson = request.Context.ToString(),
            CorrelationId = request.CorrelationId
        };

        var result = await _mediator.Send(new StartWorkflowCommand(dto), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { workflowId = result.Value });
    }

    /// <summary>
    /// Get workflow status by ID.
    /// </summary>
    [HttpGet("{workflowId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid workflowId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkflowRunQuery(workflowId), cancellationToken);
        
        if (!result.IsSuccess)
        {
            return NotFound(new { workflowId, errors = result.Errors });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get workflow execution history (logs).
    /// </summary>
    [HttpGet("{workflowId:guid}/history")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid workflowId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkflowHistoryQuery(workflowId), cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { workflowId, errors = result.Errors });
        }

        return Ok(result.Value);
    }

    [HttpPost("{workflowId:guid}/resume")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Resume(Guid workflowId, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var input = JsonSerializer.Deserialize<Dictionary<string, object>>(payload.ToString()) ?? new Dictionary<string, object>();
        var result = await _mediator.Send(new ResumeWorkflowCommand(workflowId, input), cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { workflowId = result.Value });
    }

    public record SignalRequest(string SignalName, JsonElement Payload);

    [HttpPost("{workflowId:guid}/signal")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Signal(Guid workflowId, [FromBody] SignalRequest request, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Payload.ToString()) ?? new Dictionary<string, object>();
        var result = await _mediator.Send(new SignalWorkflowCommand(workflowId, request.SignalName, payload), cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { workflowId = result.Value });
    }

    public record CancelRequest(string Reason);

    [HttpPost("{workflowId:guid}/cancel")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(Guid workflowId, [FromBody] CancelRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelWorkflowCommand(workflowId, request.Reason), cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { workflowId = result.Value });
    }
}