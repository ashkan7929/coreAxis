using CoreAxis.Modules.AuthModule.Application.Commands.Roles;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Roles;
using CoreAxis.SharedKernel.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            dto.TenantId,
            dto.PermissionIds);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetRoleByIdQuery
        return NotImplemented("Get role by ID functionality not yet implemented");
    }

    /// <summary>
    /// Get roles by tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetByTenant(
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRolesByTenantQuery(tenantId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Error);
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
        // TODO: Implement UpdateRoleCommand
        return NotImplemented("Update role functionality not yet implemented");
    }

    /// <summary>
    /// Delete role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement DeleteRoleCommand
        return NotImplemented("Delete role functionality not yet implemented");
    }

    /// <summary>
    /// Add permission to role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/permissions/{permissionId}")]
    public async Task<ActionResult> AddPermission(Guid id, Guid permissionId, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement AddPermissionToRoleCommand
        return NotImplemented("Add permission to role functionality not yet implemented");
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<ActionResult> RemovePermission(Guid id, Guid permissionId, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement RemovePermissionFromRoleCommand
        return NotImplemented("Remove permission from role functionality not yet implemented");
    }

    private ActionResult NotImplemented(string message)
    {
        return StatusCode(501, new { message });
    }
}