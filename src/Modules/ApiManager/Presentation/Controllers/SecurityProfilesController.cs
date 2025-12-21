using CoreAxis.Modules.ApiManager.Application.Commands;
using CoreAxis.Modules.ApiManager.Application.Queries;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    /// <remarks>
    /// Responses:
    /// - 200: Returns all security profiles.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Read permission).
    /// - 500: Internal error.
    /// </remarks>
    [HttpGet]
    [HasPermission("ApiManager", "Read")]
    [ProducesResponseType(typeof(List<SecurityProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to retrieve security profiles.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://coreaxis.dev/problems/apim/internal_error");
        }
    }

    /// <summary>
    /// Get security profile by ID
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 200: Returns the security profile.
    /// - 404: Not found.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Read permission).
    /// - 500: Internal error.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [HasPermission("ApiManager", "Read")]
    [ProducesResponseType(typeof(SecurityProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SecurityProfileDto>> GetSecurityProfile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _mediator.Send(new GetSecurityProfileByIdQuery(id), cancellationToken);

            if (profile == null)
            {
                return Problem(
                    title: "Not Found",
                    detail: $"Security profile with ID {id} not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://coreaxis.dev/problems/apim/not_found");
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security profile {ProfileId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to retrieve security profile.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://coreaxis.dev/problems/apim/internal_error");
        }
    }

    /// <summary>
    /// Create a new security profile
    /// </summary>
    /// <remarks>
    /// Sample request body:
    /// {
    ///   "type": "ApiKey",
    ///   "configJson": "{\"headerName\":\"X-API-Key\",\"keyValue\":null}",
    ///   "rotationPolicy": "@monthly"
    /// }
    ///
    /// Responses:
    /// - 201: Created profile.
    /// - 400: Invalid argument.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Create permission).
    /// - 500: Internal error.
    /// </remarks>
    [HttpPost]
    [HasPermission("ApiManager", "Create")]
    [ProducesResponseType(typeof(CreateSecurityProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
            return Problem(
                title: "Bad Request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://coreaxis.dev/problems/apim/bad_request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating security profile");
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to create security profile.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://coreaxis.dev/problems/apim/internal_error");
        }
    }

    /// <summary>
    /// Update a security profile
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 204: Updated successfully.
    /// - 404: Not found.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Update permission).
    /// - 500: Internal error.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [HasPermission("ApiManager", "Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateSecurityProfile(
        Guid id,
        [FromBody] UpdateSecurityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UpdateSecurityProfileCommand(
                id,
                null, // Name not exposed in request yet
                request.ConfigJson,
                request.RotationPolicy
            );

            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                return Problem(
                    title: "Not Found",
                    detail: $"Security profile with ID {id} not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://coreaxis.dev/problems/apim/not_found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security profile {ProfileId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to update security profile.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://coreaxis.dev/problems/apim/internal_error");
        }
    }

    /// <summary>
    /// Delete a security profile
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 204: Deleted successfully.
    /// - 404: Not found.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Delete permission).
    /// - 500: Internal error.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [HasPermission("ApiManager", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
                return Problem(
                    title: "Not Found",
                    detail: $"Security profile with ID {id} not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://coreaxis.dev/problems/apim/not_found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Bad Request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://coreaxis.dev/problems/apim/bad_request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting security profile {ProfileId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to delete security profile.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://coreaxis.dev/problems/apim/internal_error");
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