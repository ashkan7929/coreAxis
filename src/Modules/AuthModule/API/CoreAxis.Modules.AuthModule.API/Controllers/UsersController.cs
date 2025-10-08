using CoreAxis.Modules.AuthModule.Application.Commands.Users;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Users;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    /// <remarks>
    /// Functionality: Retrieves a user's profile by unique identifier.
    ///
    /// Output (200 OK):
    /// <code>
    /// {
    ///   "id": "9f3aa6f2-2d1e-4e4f-9b3a-1f2e7c6d4a5b",
    ///   "username": "jdoe",
    ///   "email": "jdoe@example.com"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Returns a paged list of users.
    ///
    /// Query Parameters:
    /// - pageSize (default 50)
    /// - pageNumber (default 1)
    ///
    /// Output (200 OK):
    /// <code>
    /// [ { "id": "...", "username": "..." } ]
    /// </code>
    /// </remarks>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Updates basic profile fields for a user.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "email": "jdoe@example.com",
    ///   "mobileNumber": "09123456789"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="dto">Updated user data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Deletes a user account by ID.
    ///
    /// Output (204 NoContent): No response body
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Assigns an existing role to a user.
    ///
    /// Input:
    /// <code>
    /// { "roleId": "3f0c2c1c-8c4c-4d2e-9a5b-4f1c9f5a2b6d" }
    /// </code>
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="dto">Role assignment data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Removes a role assignment from a user.
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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