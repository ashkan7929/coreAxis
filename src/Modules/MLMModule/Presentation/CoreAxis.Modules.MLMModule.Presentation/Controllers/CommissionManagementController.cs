using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Authorization;
using CoreAxis.SharedKernel.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.MLMModule.Presentation.Controllers;

[ApiController]
[Route("api/mlm/commissions")]
[Authorize]
public class CommissionManagementController : ControllerBase
{
    private readonly ICommissionManagementService _commissionManagementService;
    private readonly IValidator<GetCommissionsRequest> _getCommissionsValidator;
    private readonly IValidator<ApproveCommissionRequest> _approveCommissionValidator;
    private readonly IValidator<RejectCommissionRequest> _rejectCommissionValidator;

    public CommissionManagementController(
        ICommissionManagementService commissionManagementService,
        IValidator<GetCommissionsRequest> getCommissionsValidator,
        IValidator<ApproveCommissionRequest> approveCommissionValidator,
        IValidator<RejectCommissionRequest> rejectCommissionValidator)
    {
        _commissionManagementService = commissionManagementService;
        _getCommissionsValidator = getCommissionsValidator;
        _approveCommissionValidator = approveCommissionValidator;
        _rejectCommissionValidator = rejectCommissionValidator;
    }

    /// <summary>
    /// Gets commissions with filtering and pagination
    /// </summary>
    /// <param name="request">Filter and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of commissions</returns>
    [HttpGet]
    [RequirePermission("MLM.Commission", "Read")]
    public async Task<IActionResult> GetCommissions(
        [FromQuery] GetCommissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _getCommissionsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var result = await _commissionManagementService.GetCommissionsAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Gets commission by ID
    /// </summary>
    /// <param name="id">Commission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commission details</returns>
    [HttpGet("{id:guid}")]
    [RequirePermission("MLM.Commission", "Read")]
    public async Task<IActionResult> GetCommissionById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _commissionManagementService.GetCommissionByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>
    /// Gets user commissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Filter and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's commissions</returns>
    [HttpGet("user/{userId:guid}")]
    [RequirePermission("MLM.Commission", "Read")]
    public async Task<IActionResult> GetUserCommissions(
        Guid userId,
        [FromQuery] GetCommissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _getCommissionsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var result = await _commissionManagementService.GetUserCommissionsAsync(userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Approves a commission
    /// </summary>
    /// <param name="id">Commission ID</param>
    /// <param name="request">Approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/approve")]
    [RequirePermission("MLM.Commission", "Approve")]
    public async Task<IActionResult> ApproveCommission(
        Guid id,
        [FromBody] ApproveCommissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _approveCommissionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var userId = GetUserId();
        var result = await _commissionManagementService.ApproveCommissionAsync(id, userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Rejects a commission
    /// </summary>
    /// <param name="id">Commission ID</param>
    /// <param name="request">Rejection request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/reject")]
    [RequirePermission("MLM.Commission", "Reject")]
    public async Task<IActionResult> RejectCommission(
        Guid id,
        [FromBody] RejectCommissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _rejectCommissionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var userId = GetUserId();
        var result = await _commissionManagementService.RejectCommissionAsync(id, userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in claims");
    }
}