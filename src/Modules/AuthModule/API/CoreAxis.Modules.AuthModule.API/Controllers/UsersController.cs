using CoreAxis.Modules.AuthModule.Application.Commands.Users;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Users;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Errors);
    }

    /// <summary>
    /// Get users with pagination
    /// </summary>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(pageSize, pageNumber);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
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
        var command = new UpdateUserCommand(id, dto);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return BadRequest(result.Errors);
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
        // Set the UserId from the route parameter
        dto.UserId = id;
        var command = new AssignRoleCommand(dto);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<ActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken = default)
    {
        var command = new RemoveRoleFromUserCommand(id, roleId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(result.Errors);
    }
}