using CoreAxis.Modules.TaskModule.Application.Commands;
using CoreAxis.Modules.TaskModule.Application.DTOs;
using CoreAxis.Modules.TaskModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.TaskModule.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[ApiExplorerSettings(GroupName = "tasks")]
public class TaskController : ControllerBase
{
    private readonly IMediator _mediator;

    public TaskController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    private List<string> GetUserRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    [HttpGet("inbox")]
    [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInbox([FromQuery] string? status = "Open", CancellationToken cancellationToken = default)
    {
        var query = new GetInboxQuery(GetUserId(), GetUserRoles(), status);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTask(Guid taskId, CancellationToken cancellationToken = default)
    {
        var query = new GetTaskDetailsQuery(taskId, GetUserId(), GetUserRoles());
        var result = await _mediator.Send(query, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Errors.Contains("not found")) return NotFound(result.Errors);
            return StatusCode(403, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("{taskId:guid}/history")]
    [ProducesResponseType(typeof(List<TaskActionLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(Guid taskId, CancellationToken cancellationToken = default)
    {
        var query = new GetTaskHistoryQuery(taskId, GetUserId(), GetUserRoles());
        var result = await _mediator.Send(query, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Errors.Contains("not found")) return NotFound(result.Errors);
            return StatusCode(403, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost("{taskId:guid}/claim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Claim(Guid taskId, CancellationToken cancellationToken = default)
    {
        var command = new ClaimTaskCommand(taskId, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess) return BadRequest(result.Errors);
        return Ok();
    }

    [HttpPost("{taskId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(Guid taskId, [FromBody] TaskActionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new ApproveTaskCommand(taskId, GetUserId(), request.Comment, request.Payload);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess) return BadRequest(result.Errors);
        return Ok();
    }

    [HttpPost("{taskId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(Guid taskId, [FromBody] TaskActionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new RejectTaskCommand(taskId, GetUserId(), request.Comment);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess) return BadRequest(result.Errors);
        return Ok();
    }

    [HttpPost("{taskId:guid}/return")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Return(Guid taskId, [FromBody] TaskActionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new ReturnTaskCommand(taskId, GetUserId(), request.Comment);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess) return BadRequest(result.Errors);
        return Ok();
    }

    [HttpPost("{taskId:guid}/delegate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delegate(Guid taskId, [FromBody] DelegateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DelegateTaskCommand(taskId, GetUserId(), request.AssigneeType, request.AssigneeId, request.Comment);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess) return BadRequest(result.Errors);
        return Ok();
    }
}
