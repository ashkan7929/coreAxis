using CoreAxis.Modules.AuthModule.Application.Commands.Permissions;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Permissions;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    /// <remarks>
    /// Functionality: Returns all defined permissions in the system.
    ///
    /// Output (200 OK):
    /// <code>
    /// [ { "id": "...", "page": "Users", "action": "Read" } ]
    /// </code>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all permissions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Retrieves a permission by its identifier.
    /// </remarks>
    /// <param name="id">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Creates a permission for a Page + Action combination.
    ///
    /// Input:
    /// <code>
    /// { "page": "Users", "action": "Read", "description": "Read users" }
    /// </code>
    /// </remarks>
    /// <param name="dto">Permission creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created permission information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <remarks>
    /// Functionality: Updates a permission's metadata.
    /// </remarks>
    /// <param name="id">Permission ID</param>
    /// <param name="dto">Updated permission data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated permission information</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Deletes a permission by ID.
    /// Output (204 NoContent): No response body
    /// </remarks>
    /// <param name="id">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Lists permissions associated with given page.
    /// </remarks>
    /// <param name="pageId">Page ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions for the page</returns>
    [HttpGet("by-page/{pageId}")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Lists permissions associated with given action.
    /// </remarks>
    /// <param name="actionId">Action ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions for the action</returns>
    [HttpGet("by-action/{actionId}")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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