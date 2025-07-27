using CoreAxis.Modules.AuthModule.Application.Commands.Users;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.AuthModule.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="dto">User registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register([FromBody] CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserCommand(
            dto.Username,
            dto.Email,
            dto.Password,
            dto.TenantId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Authenticate user and get access token
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login result with token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        var command = new LoginCommand(
            dto.Username,
            dto.Password,
            dto.TenantId,
            ipAddress);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return Unauthorized(result.Errors);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="dto">Password change data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        // Get UserId and TenantId from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid user or tenant information");
        }

        var command = new ChangePasswordCommand(
            userId,
            dto.CurrentPassword,
            dto.NewPassword,
            tenantId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { message = "Password changed successfully" });
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Get user by ID (placeholder for CreatedAtAction)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        // This would typically use a GetUserByIdQuery
        // For now, return NotFound as placeholder
        return NotFound();
    }
}