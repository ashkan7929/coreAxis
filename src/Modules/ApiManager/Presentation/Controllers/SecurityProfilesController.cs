using CoreAxis.Modules.ApiManager.Application.Commands;
using CoreAxis.Modules.ApiManager.Application.Queries;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecurityProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SecurityProfilesController> _logger;

    public SecurityProfilesController(IMediator mediator, ILogger<SecurityProfilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all security profiles
    /// </summary>
    [HttpGet]
    [HasPermission("ApiManager", "Read")]
    public async Task<ActionResult<List<SecurityProfileDto>>> GetSecurityProfiles(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await _mediator.Send(new GetSecurityProfilesQuery(), cancellationToken);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security profiles");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get security profile by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("ApiManager", "Read")]
    public async Task<ActionResult<SecurityProfileDto>> GetSecurityProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _mediator.Send(new GetSecurityProfileByIdQuery(id), cancellationToken);

            if (profile == null)
            {
                return NotFound(new { message = $"Security profile with ID {id} not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security profile {ProfileId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new security profile
    /// </summary>
    [HttpPost]
    [HasPermission("ApiManager", "Create")]
    public async Task<ActionResult<CreateSecurityProfileResponse>> CreateSecurityProfile(
        [FromBody] CreateSecurityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new CreateSecurityProfileCommand(
                request.Type,
                request.ConfigJson,
                request.RotationPolicy
            );

            var profileId = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetSecurityProfile),
                new { id = profileId },
                new CreateSecurityProfileResponse(profileId)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security profile");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a security profile
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission("ApiManager", "Update")]
    public async Task<ActionResult> UpdateSecurityProfile(
        Guid id,
        [FromBody] UpdateSecurityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UpdateSecurityProfileCommand(
                id,
                request.ConfigJson,
                request.RotationPolicy
            );

            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Security profile with ID {id} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security profile {ProfileId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a security profile
    /// </summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("ApiManager", "Delete")]
    public async Task<ActionResult> DeleteSecurityProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteSecurityProfileCommand(id);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                return NotFound(new { message = $"Security profile with ID {id} not found" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting security profile {ProfileId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

// DTOs
public record SecurityProfileDto(
    Guid Id,
    string Type,
    string ConfigJson,
    string? RotationPolicy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateSecurityProfileRequest(
    string Type,
    string ConfigJson,
    string? RotationPolicy = null
);

public record CreateSecurityProfileResponse(Guid Id);

public record UpdateSecurityProfileRequest(
    string? ConfigJson = null,
    string? RotationPolicy = null
);