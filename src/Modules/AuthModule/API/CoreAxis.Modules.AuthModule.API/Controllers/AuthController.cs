using CoreAxis.Modules.AuthModule.Application.Commands.Users;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Users;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;
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
    /// Register a new user with national verification
    /// </summary>
    /// <param name="dto">User registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterUserResultDto>> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
    {
        // Birth date is received from registration API in yyyymmdd format (e.g., 13791129)
        var command = new RegisterUserCommand(
            dto.NationalCode, // Using national code as username for now
            $"{dto.NationalCode}@temp.com", // Temporary email
            "TempPassword123!", // Temporary password
            dto.NationalCode,
            dto.MobileNumber,
            dto.BirthDate, // Birth date in yyyymmdd format for Civil Registry
            dto.ReferralCode);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Create a new user (legacy endpoint)
    /// </summary>
    /// <param name="dto">User creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    [HttpPost("create-user")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateUserCommand(
            dto.Username,
            dto.Email,
            dto.Password);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Send OTP for login or registration
    /// </summary>
    /// <param name="dto">OTP request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OTP send result</returns>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<ActionResult> SendOtp([FromBody] SendOtpDto dto, CancellationToken cancellationToken = default)
    {
        var command = new SendOtpCommand(dto.MobileNumber, dto.Purpose);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { message = "OTP sent successfully" });
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Verify OTP (simple verification without login)
    /// </summary>
    /// <param name="dto">OTP verification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Simple verification result</returns>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<SimpleOtpVerificationResultDto>> VerifyOtp([FromBody] VerifyOtpDto dto, CancellationToken cancellationToken = default)
    {
        var command = new VerifyOtpCommand(dto.MobileNumber, dto.OtpCode, dto.Purpose);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new SimpleOtpVerificationResultDto { IsSuccess = result.Value.IsSuccess });
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Login with OTP and get full user information with token
    /// </summary>
    /// <param name="dto">OTP verification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete login result with user info and JWT token</returns>
    [HttpPost("login-with-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<OtpVerificationResultDto>> LoginWithOtp([FromBody] VerifyOtpDto dto, CancellationToken cancellationToken = default)
    {
        // Force purpose to Login for this endpoint
        var command = new VerifyOtpCommand(dto.MobileNumber, dto.OtpCode, OtpPurpose.Login);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Authenticate user and get access token (legacy endpoint)
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
        // Get UserId from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest("Invalid user information");
        }

        var command = new ChangePasswordCommand(
            userId,
            dto.CurrentPassword,
            dto.NewPassword);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { message = "Password changed successfully" });
        }

        return BadRequest(result.Errors);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    [HttpGet("user/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken = default)
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
    /// Check if mobile number exists and has password
    /// </summary>
    /// <param name="dto">Mobile number to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User existence and password status</returns>
    [HttpPost("check-mobile")]
    [AllowAnonymous]
    public async Task<ActionResult<CheckMobileResultDto>> CheckMobile([FromBody] CheckMobileDto dto, CancellationToken cancellationToken = default)
    {
        var query = new CheckMobileQuery(dto.MobileNumber);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }
}