using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Authorization;
using CoreAxis.SharedKernel.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers;

/// <summary>
/// Admin controller for managing formula versions (publish/unpublish).
/// </summary>
[ApiController]
[Route("api/formulas/admin")]
[Authorize]
[RequirePermission("Formulas", "manage_access")]
public class FormulaAdminController : ControllerBase
{
    private readonly IFormulaVersionRepository _formulaVersionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FormulaAdminController> _logger;

    public FormulaAdminController(
        IFormulaVersionRepository formulaVersionRepository,
        IUnitOfWork unitOfWork,
        ILogger<FormulaAdminController> logger)
    {
        _formulaVersionRepository = formulaVersionRepository ?? throw new ArgumentNullException(nameof(formulaVersionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publish a specific formula version.
    /// </summary>
    /// <param name="formulaDefinitionId">Formula definition ID.</param>
    /// <param name="versionNumber">Version number to publish.</param>
    /// <param name="request">Publish request containing publisher ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{formulaDefinitionId:guid}/versions/{versionNumber:int}/publish")]
    public async Task<IActionResult> PublishVersion(
        [FromRoute] Guid formulaDefinitionId,
        [FromRoute] int versionNumber,
        [FromBody] PublishFormulaVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        var version = await _formulaVersionRepository.GetByFormulaDefinitionIdAndVersionAsync(formulaDefinitionId, versionNumber, cancellationToken);
        if (version is null)
        {
            return BuildProblem(
                title: "Formula version not found",
                detail: $"No version '{versionNumber}' for definition '{formulaDefinitionId}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "ADM_VERSION_NOT_FOUND");
        }

        try
        {
            version.Publish(request.PublishedBy);
            await _formulaVersionRepository.UpdateAsync(version, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Published formula version {Version} for definition {DefinitionId}", versionNumber, formulaDefinitionId);

            return Ok(new
            {
                id = version.Id,
                formulaDefinitionId,
                version = versionNumber,
                isPublished = version.IsPublished,
                publishedAt = version.PublishedAt,
                publishedBy = version.PublishedBy
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to publish version {Version} for definition {DefinitionId}", versionNumber, formulaDefinitionId);
            return BuildProblem(
                title: "Publish conflict",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                code: "ADM_PUBLISH_CONFLICT");
        }
    }

    /// <summary>
    /// Unpublish a specific formula version.
    /// </summary>
    /// <param name="formulaDefinitionId">Formula definition ID.</param>
    /// <param name="versionNumber">Version number to unpublish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{formulaDefinitionId:guid}/versions/{versionNumber:int}/unpublish")]
    public async Task<IActionResult> UnpublishVersion(
        [FromRoute] Guid formulaDefinitionId,
        [FromRoute] int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var version = await _formulaVersionRepository.GetByFormulaDefinitionIdAndVersionAsync(formulaDefinitionId, versionNumber, cancellationToken);
        if (version is null)
        {
            return BuildProblem(
                title: "Formula version not found",
                detail: $"No version '{versionNumber}' for definition '{formulaDefinitionId}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "ADM_VERSION_NOT_FOUND");
        }

        try
        {
            version.Unpublish();
            await _formulaVersionRepository.UpdateAsync(version, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Unpublished formula version {Version} for definition {DefinitionId}", versionNumber, formulaDefinitionId);

            return Ok(new
            {
                id = version.Id,
                formulaDefinitionId,
                version = versionNumber,
                isPublished = version.IsPublished,
                publishedAt = version.PublishedAt,
                publishedBy = version.PublishedBy
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to unpublish version {Version} for definition {DefinitionId}", versionNumber, formulaDefinitionId);
            return BuildProblem(
                title: "Unpublish conflict",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                code: "ADM_UNPUBLISH_CONFLICT");
        }
    }

    private ObjectResult BuildProblem(string title, string detail, int statusCode, string code)
    {
        var traceId = HttpContext.TraceIdentifier;
        var correlationId = HttpContext.GetCorrelationId();

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = $"https://errors.coreaxis.dev/formula/{code}"
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(correlationId)) problem.Extensions["correlationId"] = correlationId;

        Response.ContentType = "application/problem+json";
        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}

/// <summary>
/// Request payload to publish a formula version.
/// </summary>
public class PublishFormulaVersionRequest
{
    /// <summary>
    /// User ID who publishes the version.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    public Guid PublishedBy { get; set; }
}