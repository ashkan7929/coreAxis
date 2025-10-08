using CoreAxis.Modules.AuthModule.Application.Commands.Users;
using CoreAxis.Modules.AuthModule.Application.DTOs;
using CoreAxis.Modules.AuthModule.Application.Queries.Users;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    /// <remarks>
    /// Functionality: Registers a user using civil registry verification. BirthDate must be in Persian yyyymmdd.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "nationalCode": "1234567890",
    ///   "mobileNumber": "09123456789",
    ///   "birthDate": "13791129",
    ///   "referralCode": "ABC123"
    /// }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// {
    ///   "userId": "3f0c2c1c-8c4c-4d2e-9a5b-4f1c9f5a2b6d",
    ///   "username": "1234567890",
    ///   "token": "<jwt-token>",
    ///   "expiresIn": 3600
    /// }
    /// </code>
    /// </remarks>
    /// <param name="dto">User registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterUserResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <remarks>
    /// Functionality: Creates a user with username/password (legacy). Prefer OTP-based endpoints.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "username": "jdoe",
    ///   "email": "jdoe@example.com",
    ///   "password": "Str0ngP@ss!"
    /// }
    /// </code>
    ///
    /// Output (201 Created):
    /// <code>
    /// {
    ///   "id": "9f3aa6f2-2d1e-4e4f-9b3a-1f2e7c6d4a5b",
    ///   "username": "jdoe",
    ///   "email": "jdoe@example.com"
    /// }
    /// </code>
    /// </remarks>
    /// <param name="dto">User creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    [HttpPost("create-user")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <remarks>
    /// Functionality: Sends a one-time password to the provided mobile number for Login or Register.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "mobileNumber": "09123456789",
    ///   "purpose": "Login"
    /// }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// { "message": "OTP sent successfully" }
    /// </code>
    /// </remarks>
    /// <param name="dto">OTP request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OTP send result</returns>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <remarks>
    /// Functionality: Verifies the OTP code. Returns only a boolean success flag.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "mobileNumber": "09123456789",
    ///   "otpCode": "123456",
    ///   "purpose": "Register"
    /// }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// { "isSuccess": true }
    /// </code>
    /// </remarks>
    /// <param name="dto">OTP verification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Simple verification result</returns>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SimpleOtpVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <remarks>
    /// Functionality: Verifies OTP for Login and returns user profile with JWT token.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "mobileNumber": "09123456789",
    ///   "otpCode": "123456"
    /// }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// {
    ///   "isSuccess": true,
    ///   "user": { "id": "...", "username": "..." },
    ///   "token": "<jwt-token>",
    ///   "expiresIn": 3600
    /// }
    /// </code>
    /// </remarks>
    /// <param name="dto">OTP verification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete login result with user info and JWT token</returns>
    [HttpPost("login-with-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OtpVerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// <remarks>
    /// Functionality: Authenticates using password and returns JWT token.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "mobileNumber": "09123456789",
    ///   "password": "Str0ngP@ss!"
    /// }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// {
    ///   "token": "<jwt-token>",
    ///   "expiresIn": 3600,
    ///   "user": { "id": "...", "username": "..." }
    /// }
    /// </code>
    /// </remarks>
    /// <param name="dto">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login result with token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        var command = new LoginCommand(
            dto.MobileNumber,
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
    /// <remarks>
    /// Functionality: Changes the authenticated user's password.
    ///
    /// Input:
    /// <code>
    /// {
    ///   "currentPassword": "OldP@ss1",
    ///   "newPassword": "N3wP@ss2"
    /// }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// { "message": "Password changed successfully" }
    /// </code>
    /// </remarks>
    /// <param name="dto">Password change data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Retrieves a user's profile by ID. Requires authentication.
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
    [HttpGet("user/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <remarks>
    /// Functionality: Checks whether a user exists for the given mobile number and if a password is set.
    ///
    /// Input:
    /// <code>
    /// { "mobileNumber": "09123456789" }
    /// </code>
    ///
    /// Output (200 OK):
    /// <code>
    /// { "exists": true, "hasPassword": false }
    /// </code>
    /// </remarks>
    /// <param name="dto">Mobile number to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User existence and password status</returns>
    [HttpPost("check-mobile")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckMobileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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