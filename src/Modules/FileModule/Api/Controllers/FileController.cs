using CoreAxis.Modules.FileModule.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.FileModule.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly FileService _fileService;

    public FileController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        using var stream = file.OpenReadStream();
        var metadata = await _fileService.UploadFileAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            tenantId,
            userId);

        return Ok(metadata);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var (stream, metadata) = await _fileService.GetFileAsync(id, tenantId);

        if (stream == null || metadata == null)
            return NotFound();

        return File(stream, metadata.ContentType, metadata.FileName);
    }

    [HttpGet("{id}/metadata")]
    public async Task<IActionResult> GetMetadata(Guid id)
    {
        var metadata = await _fileService.GetFileMetadataAsync(id);
        if (metadata == null)
            return NotFound();

        return Ok(metadata);
    }
}
