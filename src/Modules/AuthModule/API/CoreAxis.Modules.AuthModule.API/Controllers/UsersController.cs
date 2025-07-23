using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Users;
using CoreAxis.SharedKernel.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.AuthModule.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        var query = new GetUserByIdQuery(id, tenantId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Error);
    }

    /// <summary>
    /// Get users by tenant with pagination
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetByTenant(
        [FromQuery] Guid tenantId,
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersByTenantQuery(tenantId, pageSize, pageNumber);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Error);
    }

    /// <summary>
    /// Update user information
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">Updated user data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        // TODO: Implement UpdateUserCommand
        return NotImplemented("Update user functionality not yet implemented");
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement DeleteUserCommand
        return NotImplemented("Delete user functionality not yet implemented");
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">Role assignment data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/roles")]
    public async Task<ActionResult> AssignRole(Guid id, [FromBody] AssignRoleDto dto, CancellationToken cancellationToken = default)
    {
        // TODO: Implement AssignRoleToUserCommand
        return NotImplemented("Assign role functionality not yet implemented");
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="roleId">Role ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<ActionResult> RemoveRole(Guid id, Guid roleId, [FromQuery] Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement RemoveRoleFromUserCommand
        return NotImplemented("Remove role functionality not yet implemented");
    }

    private ActionResult NotImplemented(string message)
    {
        return StatusCode(501, new { message });
    }
}