using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Mime;
using CoreAxis.Modules.Workflow.Api.Filters;

namespace CoreAxis.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/admin/workflows")]
[ApiExplorerSettings(GroupName = "workflows-admin")]
[Authorize]
public class WorkflowAdminController : ControllerBase
{
    private readonly IWorkflowAdminService _service;

    public WorkflowAdminController(IWorkflowAdminService service)
    {
        _service = service;
    }

    /// <summary>
    /// List workflow definitions.
    /// </summary>
    /// <remarks>
    /// Returns all workflow definitions with basic metadata.
    ///
    /// Responses:
    /// - 200 OK → array of definitions
    ///   ```json
    ///   [
    ///     { "id": "...", "code": "post-finalize", "name": "Post Finalize", "createdAt": "2024-01-01T10:00:00Z" }
    ///   ]
    ///   ```
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var defs = await _service.ListDefinitionsAsync(ct);
        var result = defs.Select(d => new { id = d.Id, code = d.Code, name = d.Name, createdAt = d.CreatedAt });
        return Ok(result);
    }

    public record CreateWorkflowRequest(string Code, string Name, string? Description);

    /// <summary>
    /// Create a new workflow definition.
    /// </summary>
    /// <remarks>
    /// Headers:
    /// - `Authorization: Bearer <token>`
    /// - `Idempotency-Key: <unique-key>` (optional)
    ///
    /// Request body example:
    /// ```json
    /// { "code": "post-finalize", "name": "Post Finalize", "description": "Runs after order finalization" }
    /// ```
    ///
    /// Responses:
    /// - 201 Created → `{ id }`
    /// - 400 BadRequest → validation errors
    /// - 401 Unauthorized
    /// - 409 Conflict → duplicate code
    /// - 500 InternalServerError
    /// </remarks>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest req, CancellationToken ct)
    {
        var created = await _service.CreateDefinitionAsync(req.Code, req.Name, req.Description, User.Identity?.Name ?? "system", ct);
        return Created($"/api/admin/workflows/{created.Id}", new { id = created.Id });
    }

    public record CreateVersionRequest(int VersionNumber, JsonElement DslJson, string? Changelog);

    /// <summary>
    /// Create a new version for an existing workflow definition.
    /// </summary>
    /// <remarks>
    /// Headers:
    /// - `Authorization: Bearer <token>`
    /// - `Idempotency-Key: <unique-key>` (optional)
    ///
    /// Request body example:
    /// ```json
    /// { "versionNumber": 2, "dslJson": { "steps": [ { "name": "SendEmail" } ] }, "changelog": "Add SendEmail" }
    /// ```
    ///
    /// Responses:
    /// - 201 Created → `{ id, version }`
    /// - 400 BadRequest → validation errors
    /// - 401 Unauthorized
    /// - 404 NotFound → definition not found
    /// - 500 InternalServerError
    /// </remarks>
    [HttpPost("{id:guid}/versions")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateVersionRequest req, CancellationToken ct)
    {
        var ver = await _service.CreateVersionAsync(id, req.VersionNumber, req.DslJson.GetRawText(), req.Changelog, User.Identity?.Name ?? "system", ct);
        return Created($"/api/admin/workflows/{id}/versions/{ver.VersionNumber}", new { id = ver.Id, version = ver.VersionNumber });
    }

    /// <summary>
    /// Publish a specific workflow version.
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 200 OK → `{ id, version, published: true }`
    /// - 404 NotFound → definition/version not found
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    [HttpPost("{id:guid}/versions/{version:int}/publish")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> Publish(Guid id, int version, CancellationToken ct)
    {
        var ok = await _service.PublishVersionAsync(id, version, ct);
        if (!ok) return NotFound();
        return Ok(new { id, version, published = true });
    }

    /// <summary>
    /// Unpublish a specific workflow version.
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 200 OK → `{ id, version, published: false }`
    /// - 404 NotFound → definition/version not found
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    [HttpPost("{id:guid}/versions/{version:int}/unpublish")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> Unpublish(Guid id, int version, CancellationToken ct)
    {
        var ok = await _service.UnpublishVersionAsync(id, version, ct);
        if (!ok) return NotFound();
        return Ok(new { id, version, published = false });
    }

    public record DryRunRequest(JsonElement InputContext);

    /// <summary>
    /// Execute a dry-run for a workflow version.
    /// </summary>
    /// <remarks>
    /// Simulates execution with the provided `inputContext` without side effects.
    ///
    /// Request body example:
    /// ```json
    /// { "inputContext": { "orderId": "...", "finalizedAt": "2024-01-01T10:00:00Z" } }
    /// ```
    ///
    /// Responses:
    /// - 200 OK → dry-run result payload
    /// - 401 Unauthorized
    /// - 404 NotFound → definition/version not found
    /// - 500 InternalServerError
    /// </remarks>
    [HttpPost("{id:guid}/versions/{version:int}/dry-run")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> DryRun(Guid id, int version, [FromBody] DryRunRequest req, CancellationToken ct)
    {
        var result = await _service.DryRunAsync(id, version, req.InputContext.GetRawText(), ct);
        return Ok(result);
    }
}