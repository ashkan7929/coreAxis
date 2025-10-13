using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var defs = await _service.ListDefinitionsAsync(ct);
        var result = defs.Select(d => new { id = d.Id, code = d.Code, name = d.Name, createdAt = d.CreatedAt });
        return Ok(result);
    }

    public record CreateWorkflowRequest(string Code, string Name, string? Description);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest req, CancellationToken ct)
    {
        var created = await _service.CreateDefinitionAsync(req.Code, req.Name, req.Description, User.Identity?.Name ?? "system", ct);
        return Created($"/api/admin/workflows/{created.Id}", new { id = created.Id });
    }

    public record CreateVersionRequest(int VersionNumber, JsonElement DslJson, string? Changelog);

    [HttpPost("{id:guid}/versions")]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateVersionRequest req, CancellationToken ct)
    {
        var ver = await _service.CreateVersionAsync(id, req.VersionNumber, req.DslJson.GetRawText(), req.Changelog, User.Identity?.Name ?? "system", ct);
        return Created($"/api/admin/workflows/{id}/versions/{ver.VersionNumber}", new { id = ver.Id, version = ver.VersionNumber });
    }

    [HttpPost("{id:guid}/versions/{version:int}/publish")]
    public async Task<IActionResult> Publish(Guid id, int version, CancellationToken ct)
    {
        var ok = await _service.PublishVersionAsync(id, version, ct);
        if (!ok) return NotFound();
        return Ok(new { id, version, published = true });
    }

    [HttpPost("{id:guid}/versions/{version:int}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, int version, CancellationToken ct)
    {
        var ok = await _service.UnpublishVersionAsync(id, version, ct);
        if (!ok) return NotFound();
        return Ok(new { id, version, published = false });
    }

    public record DryRunRequest(JsonElement InputContext);

    [HttpPost("{id:guid}/versions/{version:int}/dry-run")]
    public async Task<IActionResult> DryRun(Guid id, int version, [FromBody] DryRunRequest req, CancellationToken ct)
    {
        var result = await _service.DryRunAsync(id, version, req.InputContext.GetRawText(), ct);
        return Ok(result);
    }
}