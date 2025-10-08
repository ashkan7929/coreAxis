using CoreAxis.Modules.AuthModule.Application.Commands.Roles;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Roles;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.AuthModule.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <remarks>
    /// Functionality: Creates a role with a name, description, and permissions.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "name": "Admin",
    ///   "description": "System administrator",
    ///   "permissionIds": ["...", "..."]
    /// }
    /// </code>
    ///
    /// Output (201 Created): RoleDto
    /// </remarks>
    /// <param name="dto">Role creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created role information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateRoleCommand(
            dto.Name,
            dto.Description,
            dto.PermissionIds);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <remarks>
    /// Functionality: Retrieves a role and its permissions by ID.
    /// </remarks>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetRoleByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Get roles
    /// </summary>
    /// <remarks>
    /// Functionality: Returns all roles available in the system.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles(
        CancellationToken cancellationToken = default)
    {
        var query = new GetRolesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Update role information
    /// </summary>
    /// <remarks>
    /// Functionality: Updates a role's metadata and permissions.
    /// </remarks>
    /// <param name="id">Role ID</param>
    /// <param name="dto">Updated role data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated role information</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken = default)
    {
        var command = new UpdateRoleCommand(id, dto);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Delete role
    /// </summary>
    /// <remarks>
    /// Functionality: Deletes a role by ID.
    /// Output (204 NoContent): No response body
    /// </remarks>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteRoleCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Add permission to role
    /// </summary>
    /// <remarks>
    /// Functionality: Adds a permission to a role.
    /// </remarks>
    /// <param name="id">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddPermission(Guid id, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var command = new AddPermissionToRoleCommand(id, permissionId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    /// <remarks>
    /// Functionality: Removes a permission from a role.
    /// </remarks>
    /// <param name="id">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> RemovePermission(Guid id, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var command = new RemovePermissionFromRoleCommand(id, permissionId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(result.Errors);
    }
}