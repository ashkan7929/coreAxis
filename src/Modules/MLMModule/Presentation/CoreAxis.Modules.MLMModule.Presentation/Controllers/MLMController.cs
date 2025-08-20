using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.SharedKernel.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CoreAxis.Modules.MLMModule.Presentation.Controllers;

[ApiController]
[Route("api/mlm")]
[Authorize]
public class MLMController : ControllerBase
{
    private readonly IMLMService _mlmService;
    private readonly ILogger<MLMController> _logger;
    private readonly IValidator<JoinMLMRequest> _joinMLMValidator;
    private readonly IValidator<GetDownlineRequest> _getDownlineValidator;

    public MLMController(
        IMLMService mlmService,
        ILogger<MLMController> logger,
        IValidator<JoinMLMRequest> joinMLMValidator,
        IValidator<GetDownlineRequest> getDownlineValidator)
    {
        _mlmService = mlmService;
        _logger = logger;
        _joinMLMValidator = joinMLMValidator;
        _getDownlineValidator = getDownlineValidator;
    }

    /// <summary>
    /// Join MLM network with referral code
    /// </summary>
    /// <param name="request">Join MLM request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User referral information</returns>
    [HttpPost("join")]
    [RequirePermission("MLM.Network", "Create")]
    public async Task<IActionResult> JoinMLM(
        [FromBody] JoinMLMRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _joinMLMValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mlmService.JoinMLMAsync(userId, request);
        
        return CreatedAtAction(nameof(GetUserReferralInfo), new { userId }, result);
    }

    /// <summary>
    /// Get user referral information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User referral information</returns>
    [HttpGet("users/{userId}/referral-info")]
    [RequirePermission("MLM.Network", "Read")]
    public async Task<IActionResult> GetUserReferralInfo(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mlmService.GetUserReferralInfoAsync(userId);
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Get user downline with pagination
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Downline request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated downline list</returns>
    [HttpGet("users/{userId}/downline")]
    [RequirePermission("MLM.Network", "Read")]
    public async Task<IActionResult> GetDownline(
        Guid userId,
        [FromQuery] GetDownlineRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _getDownlineValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var result = await _mlmService.GetDownlineAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// Get current user's referral information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user's referral information</returns>
    [HttpGet("my-referral-info")]
    [RequirePermission("MLM.Network", "Read")]
    public async Task<IActionResult> GetMyReferralInfo(CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mlmService.GetUserReferralInfoAsync(userId);
        
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Get current user's downline
    /// </summary>
    /// <param name="request">Downline request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user's downline</returns>
    [HttpGet("my-downline")]
    [RequirePermission("MLM.Network", "Read")]
    public async Task<IActionResult> GetMyDownline(
        [FromQuery] GetDownlineRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _getDownlineValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mlmService.GetDownlineAsync(userId, request);
        
        return Ok(result);
    }
}