using CoreAxis.Modules.AuthModule.Application.Commands.Permissions;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Permissions;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.AuthModule.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all permissions</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var query = new GetAllPermissionsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PermissionDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetPermissionByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    /// <param name="dto">Permission creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created permission information</returns>
    [HttpPost]
    public async Task<ActionResult<PermissionDto>> Create([FromBody] CreatePermissionDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreatePermissionCommand(dto);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Update permission information
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="dto">Updated permission data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated permission information</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<PermissionDto>> Update(Guid id, [FromBody] UpdatePermissionDto dto, CancellationToken cancellationToken = default)
    {
        var command = new UpdatePermissionCommand(id, dto);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    /// <param name="id">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeletePermissionCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Get permissions by page
    /// </summary>
    /// <param name="pageId">Page ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions for the page</returns>
    [HttpGet("by-page/{pageId}")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetByPage(Guid pageId, CancellationToken cancellationToken = default)
    {
        var query = new GetPermissionsByPageQuery(pageId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Get permissions by action
    /// </summary>
    /// <param name="actionId">Action ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions for the action</returns>
    [HttpGet("by-action/{actionId}")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetByAction(Guid actionId, CancellationToken cancellationToken = default)
    {
        var query = new GetPermissionsByActionQuery(actionId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }
}