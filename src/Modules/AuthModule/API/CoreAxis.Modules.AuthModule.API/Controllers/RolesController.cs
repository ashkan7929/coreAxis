using CoreAxis.Modules.AuthModule.Application.Commands.Roles;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Roles;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    /// <param name="dto">Role creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created role information</returns>
    [HttpPost]
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
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role information</returns>
    [HttpGet("{id}")]
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    [HttpGet]
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
    /// <param name="id">Role ID</param>
    /// <param name="dto">Updated role data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated role information</returns>
    [HttpPut("{id}")]
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
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
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
    /// <param name="id">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/permissions/{permissionId}")]
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
    /// <param name="id">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/permissions/{permissionId}")]
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