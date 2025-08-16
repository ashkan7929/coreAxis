using CoreAxis.Modules.ApiManager.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityProfilesController : ControllerBase
{
    private readonly DbContext _dbContext;
    private readonly ILogger<SecurityProfilesController> _logger;

    public SecurityProfilesController(DbContext dbContext, ILogger<SecurityProfilesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all security profiles
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SecurityProfileDto>>> GetSecurityProfiles(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await _dbContext.Set<SecurityProfile>()
                .Select(sp => new SecurityProfileDto(
                    sp.Id,
                    sp.Type.ToString(),
                    sp.ConfigJson,
                    sp.RotationPolicy,
                    sp.CreatedAt,
                    sp.UpdatedAt
                ))
                .ToListAsync(cancellationToken);

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
    public async Task<ActionResult<SecurityProfileDto>> GetSecurityProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _dbContext.Set<SecurityProfile>()
                .Where(sp => sp.Id == id)
                .Select(sp => new SecurityProfileDto(
                    sp.Id,
                    sp.Type.ToString(),
                    sp.ConfigJson,
                    sp.RotationPolicy,
                    sp.CreatedAt,
                    sp.UpdatedAt
                ))
                .FirstOrDefaultAsync(cancellationToken);

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
    public async Task<ActionResult<CreateSecurityProfileResponse>> CreateSecurityProfile(
        [FromBody] CreateSecurityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<SecurityType>(request.Type, out var securityType))
            {
                return BadRequest(new { message = $"Invalid security type: {request.Type}" });
            }

            var profile = new SecurityProfile(
                securityType,
                request.ConfigJson,
                request.RotationPolicy
            );

            _dbContext.Set<SecurityProfile>().Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created security profile {Type} with ID {Id}", 
                request.Type, profile.Id);

            var response = new CreateSecurityProfileResponse(profile.Id);
            return CreatedAtAction(nameof(GetSecurityProfile), new { id = profile.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security profile");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing security profile
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateSecurityProfile(
        Guid id,
        [FromBody] UpdateSecurityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _dbContext.Set<SecurityProfile>()
                .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);

            if (profile == null)
            {
                return NotFound(new { message = $"Security profile with ID {id} not found" });
            }

            profile.Update(
                profile.Type,
                !string.IsNullOrEmpty(request.ConfigJson) ? request.ConfigJson : profile.ConfigJson,
                !string.IsNullOrEmpty(request.RotationPolicy) ? request.RotationPolicy : profile.RotationPolicy
            );

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated security profile {ProfileId}", id);

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
    public async Task<ActionResult> DeleteSecurityProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _dbContext.Set<SecurityProfile>()
                .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);

            if (profile == null)
            {
                return NotFound(new { message = $"Security profile with ID {id} not found" });
            }

            // Check if profile is being used by any web services
            var isInUse = await _dbContext.Set<WebService>()
                .AnyAsync(ws => ws.SecurityProfileId == id, cancellationToken);

            if (isInUse)
            {
                return BadRequest(new { message = "Cannot delete security profile that is in use by web services" });
            }

            _dbContext.Set<SecurityProfile>().Remove(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted security profile {ProfileId}", id);

            return NoContent();
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