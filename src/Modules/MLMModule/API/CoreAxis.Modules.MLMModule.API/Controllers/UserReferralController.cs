using CoreAxis.Modules.MLMModule.Application.Commands;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.MLMModule.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserReferralController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserReferralController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new user referral
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserReferralDto>> CreateUserReferral([FromBody] CreateUserReferralDto request)
    {
        var command = new CreateUserReferralCommand
        {
            UserId = request.UserId,
            ParentUserId = request.ParentUserId
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUserReferral), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get user referral by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserReferralDto>> GetUserReferral(Guid id)
    {
        var query = new GetUserReferralByIdQuery { UserReferralId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get user referral by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<UserReferralDto>> GetUserReferralByUserId(Guid userId)
    {
        var query = new GetUserReferralByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get user referral children
    /// </summary>
    [HttpGet("{userId}/children")]
    public async Task<ActionResult<IEnumerable<UserReferralDto>>> GetUserReferralChildren(Guid userId)
    {
        var query = new GetUserReferralChildrenQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get user upline
    /// </summary>
    [HttpGet("{userId}/upline")]
    public async Task<ActionResult<IEnumerable<UserReferralDto>>> GetUserUpline(Guid userId, [FromQuery] int? levels = null)
    {
        var query = new GetUserUplineQuery { UserId = userId, MaxLevels = levels };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get user downline
    /// </summary>
    [HttpGet("{userId}/downline")]
    public async Task<ActionResult<IEnumerable<UserReferralDto>>> GetUserDownline(Guid userId, [FromQuery] int? levels = null)
    {
        var query = new GetUserDownlineQuery { UserId = userId, MaxLevels = levels };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get MLM network statistics
    /// </summary>
    [HttpGet("{userId}/network-stats")]
    public async Task<ActionResult<MLMNetworkStatsDto>> GetMLMNetworkStats(Guid userId)
    {
        var query = new GetMLMNetworkStatsQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get network tree
    /// </summary>
    [HttpGet("{userId}/network-tree")]
    public async Task<ActionResult<NetworkTreeDto>> GetNetworkTree(Guid userId, [FromQuery] int? maxDepth = null)
    {
        var query = new GetNetworkTreeQuery 
        { 
            UserId = userId, 
            MaxDepth = maxDepth ?? 5
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Update user referral
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserReferralDto>> UpdateUserReferral(Guid id, [FromBody] UpdateUserReferralDto request)
    {
        var command = new UpdateUserReferralCommand
        {
            Id = id,
            ParentUserId = request.ParentUserId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Activate user referral
    /// </summary>
    [HttpPost("{userId}/activate")]
    public async Task<ActionResult<bool>> ActivateUserReferral(Guid userId)
    {
        var command = new ActivateUserReferralCommand { UserId = userId };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate user referral
    /// </summary>
    [HttpPost("{userId}/deactivate")]
    public async Task<ActionResult<bool>> DeactivateUserReferral(Guid userId)
    {
        var command = new DeactivateUserReferralCommand { UserId = userId };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete user referral
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUserReferral(Guid id)
    {
        var command = new DeleteUserReferralCommand { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }


}