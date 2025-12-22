using CoreAxis.Modules.SecretsModule.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.SecretsModule.Api.Controllers;

[ApiController]
[Route("api/admin/secrets")]
// [Authorize(Roles = "Admin")] // Uncomment when Auth is fully integrated and roles are set
public class SecretsController : ControllerBase
{
    private readonly SecretService _secretService;

    public SecretsController(SecretService secretService)
    {
        _secretService = secretService;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var secrets = await _secretService.ListSecretsAsync(HttpContext.RequestAborted);
        return Ok(secrets);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        // NOTE: In a real system, we might NOT want to expose the decrypted value via API 
        // unless strictly necessary for debugging by super-admin.
        // Usually secrets are only resolved internally.
        // For this task, I'll allow it but warn.
        var value = await _secretService.GetSecretAsync(key, HttpContext.RequestAborted);
        if (value == null) return NotFound();
        return Ok(new { Key = key, Value = value });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Set(string key, [FromBody] SetSecretRequest request)
    {
        await _secretService.SetSecretAsync(key, request.Value, request.Description, HttpContext.RequestAborted);
        return Ok();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _secretService.DeleteSecretAsync(key, HttpContext.RequestAborted);
        return NoContent();
    }
}

public class SetSecretRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
