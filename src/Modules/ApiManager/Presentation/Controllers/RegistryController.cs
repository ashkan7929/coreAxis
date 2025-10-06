using CoreAxis.Modules.ApiManager.Infrastructure;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.AuthModule.API.Authz;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.OutputCaching;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/apimanager/registry")]
[Authorize]
public class RegistryController : ControllerBase
{
    private readonly ApiManagerDbContext _db;
    private readonly ILogger<RegistryController> _logger;
    private readonly IOutboxService _outbox;

    public RegistryController(ApiManagerDbContext db, ILogger<RegistryController> logger, IOutboxService outbox)
    {
        _db = db;
        _logger = logger;
        _outbox = outbox;
    }

    private static JsonElement SanitizeConfigJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return JsonDocument.Parse("{}").RootElement.Clone();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, object?>();
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                var key = p.Name;
                var lower = key.ToLowerInvariant();
                if (lower.Contains("secret") || lower.Contains("password") || lower.Contains("apikey") || lower.Contains("token"))
                {
                    dict[key] = null; // strip secret values
                }
                else
                {
                    dict[key] = p.Value.ValueKind switch
                    {
                        JsonValueKind.String => p.Value.GetString(),
                        JsonValueKind.Number => p.Value.TryGetInt64(out var i) ? i : p.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => JsonSerializer.Deserialize<object>(p.Value.GetRawText())
                    };
                }
            }
            var sanitized = JsonSerializer.SerializeToElement(dict);
            return sanitized;
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }

    [HttpGet("export")]
    [OutputCache(Duration = 60, VaryByHeaderNames = new[] { "Authorization" })]
    [HasPermission("ApiManager", "Admin")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        try
        {
            var services = await _db.WebServices
                .Include(ws => ws.SecurityProfile)
                .Include(ws => ws.Methods)
                    .ThenInclude(m => m.Parameters)
                .OrderBy(ws => ws.Name)
                .ToListAsync(ct);

            var export = new
            {
                Version = "v1",
                ExportedAt = DateTime.UtcNow,
                WebServices = services.Select(ws => new
                {
                    ws.Name,
                    ws.BaseUrl,
                    ws.Description,
                    ws.IsActive,
                    SecurityProfile = ws.SecurityProfile is null ? null : new
                    {
                        Type = ws.SecurityProfile.Type.ToString(),
                        Config = SanitizeConfigJson(ws.SecurityProfile.ConfigJson),
                        RotationPolicy = ws.SecurityProfile.RotationPolicy
                    },
                    Methods = ws.Methods.Select(m => new
                    {
                        m.Path,
                        m.HttpMethod,
                        m.TimeoutMs,
                        m.RetryPolicyJson,
                        m.CircuitPolicyJson,
                        Parameters = m.Parameters.Select(p => new
                        {
                            p.Name,
                            Location = p.Location.ToString(),
                            Type = p.Type,
                            p.IsRequired,
                            p.DefaultValue
                        })
                    })
                })
            };

            return Ok(export);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting registry");
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to export registry.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://coreaxis.dev/problems/apim/internal_error");
        }
    }

    public class RegistryImportModel
    {
        public List<ServiceModel> WebServices { get; set; } = new();
    }

    public class ServiceModel
    {
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SecurityModel? SecurityProfile { get; set; }
        public List<MethodModel> Methods { get; set; } = new();
    }

    public class SecurityModel
    {
        public string Type { get; set; } = "None";
        public object? Config { get; set; }
        public string? RotationPolicy { get; set; }
    }

    public class MethodModel
    {
        public string Path { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "GET";
        public int TimeoutMs { get; set; } = 30000;
        public string? RetryPolicyJson { get; set; }
        public string? CircuitPolicyJson { get; set; }
        public List<ParamModel> Parameters { get; set; } = new();
    }

    public class ParamModel
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = "Query";
        public string Type { get; set; } = "string";
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
    }

    [HttpPost("import")]
    [HasPermission("ApiManager", "Admin")]
    public async Task<IActionResult> Import([FromBody] RegistryImportModel model, CancellationToken ct)
    {
        if (model == null || model.WebServices == null || model.WebServices.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid import payload",
                Status = StatusCodes.Status400BadRequest,
                Detail = "WebServices array is required"
            });
        }

        var changes = new List<object>();

        foreach (var svc in model.WebServices)
        {
            var existing = await _db.WebServices.Include(ws => ws.Methods).FirstOrDefaultAsync(ws => ws.Name == svc.Name, ct);
            Guid? securityProfileId = null;

            if (svc.SecurityProfile != null)
            {
                var typeParsed = Enum.TryParse<SecurityType>(svc.SecurityProfile.Type, true, out var st) ? st : SecurityType.None;

                // Prevent secret values in import payloads
                try
                {
                    var raw = svc.SecurityProfile.Config?.ToString() ?? "{}";
                    using var doc = JsonDocument.Parse(raw);
                    foreach (var p in doc.RootElement.EnumerateObject())
                    {
                        var lower = p.Name.ToLowerInvariant();
                        if ((lower.Contains("secret") || lower.Contains("password") || lower.Contains("apikey") || lower.Contains("token"))
                            && p.Value.ValueKind != JsonValueKind.Null)
                        {
                            return Problem(
                                title: "Bad Request",
                                detail: $"Secret-like field '{p.Name}' must be null or omitted in import payload.",
                                statusCode: StatusCodes.Status400BadRequest,
                                type: "https://coreaxis.dev/problems/apim/secret_value_not_allowed");
                        }
                    }
                }
                catch
                {
                    // ignore parse errors; sanitization will handle
                }

                var sanitized = SanitizeConfigJson(svc.SecurityProfile.Config?.ToString());
                var cfgJson = JsonSerializer.Serialize(sanitized);
                var profile = await _db.SecurityProfiles.FirstOrDefaultAsync(p => p.Type == typeParsed && p.ConfigJson == cfgJson, ct);
                if (profile is null)
                {
                    profile = new SecurityProfile(typeParsed, cfgJson, svc.SecurityProfile.RotationPolicy);
                    _db.SecurityProfiles.Add(profile);
                    await _db.SaveChangesAsync(ct);
                }
                securityProfileId = profile.Id;
            }

            if (existing is null)
            {
                var ws = new WebService(svc.Name, svc.BaseUrl, svc.Description, securityProfileId);
                _db.WebServices.Add(ws);
                await _db.SaveChangesAsync(ct);

                foreach (var mm in svc.Methods)
                {
                    var method = new WebServiceMethod(ws.Id, mm.Path, mm.HttpMethod, mm.TimeoutMs, null, null, mm.RetryPolicyJson, mm.CircuitPolicyJson);
                    _db.WebServiceMethods.Add(method);
                    await _db.SaveChangesAsync(ct);
                    foreach (var p in mm.Parameters)
                    {
                        var locParsed = Enum.TryParse<ParameterLocation>(p.Location, true, out var loc) ? loc : ParameterLocation.Query;
                        var prm = new WebServiceParam(method.Id, p.Name, loc, p.Type, p.IsRequired, p.DefaultValue);
                        _db.WebServiceParams.Add(prm);
                    }
                }
                await _db.SaveChangesAsync(ct);

                changes.Add(new { Action = "CreatedService", Service = svc.Name });
            }
            else
            {
                // Update basics; do not touch secrets
                var updated = false;
                if (existing.BaseUrl != svc.BaseUrl || existing.Description != svc.Description)
                {
                    // domain lacks setters; rely on EF tracking via raw update is not available
                    // For simplicity in this import, recreate minimal updates via context entry
                    existing.GetType().GetProperty("BaseUrl")?.SetValue(existing, svc.BaseUrl);
                    existing.GetType().GetProperty("Description")?.SetValue(existing, svc.Description);
                    existing.GetType().GetProperty("SecurityProfileId")?.SetValue(existing, securityProfileId);
                    existing.GetType().GetProperty("UpdatedAt")?.SetValue(existing, DateTime.UtcNow);
                    updated = true;
                }

                // Upsert methods by Path+HttpMethod
                foreach (var mm in svc.Methods)
                {
                    var existingMethod = existing.Methods.FirstOrDefault(m => m.Path == mm.Path && m.HttpMethod.Equals(mm.HttpMethod, StringComparison.OrdinalIgnoreCase));
                    if (existingMethod is null)
                    {
                        var method = new WebServiceMethod(existing.Id, mm.Path, mm.HttpMethod, mm.TimeoutMs, null, null, mm.RetryPolicyJson, mm.CircuitPolicyJson);
                        _db.WebServiceMethods.Add(method);
                        await _db.SaveChangesAsync(ct);
                        foreach (var p in mm.Parameters)
                        {
                            var locParsed = Enum.TryParse<ParameterLocation>(p.Location, true, out var loc) ? loc : ParameterLocation.Query;
                            var prm = new WebServiceParam(method.Id, p.Name, loc, p.Type, p.IsRequired, p.DefaultValue);
                            _db.WebServiceParams.Add(prm);
                        }
                        changes.Add(new { Action = "AddedMethod", Service = svc.Name, Path = mm.Path, HttpMethod = mm.HttpMethod });
                    }
                    else
                    {
                        existingMethod.Update(mm.Path, mm.HttpMethod, mm.TimeoutMs, null, null, mm.RetryPolicyJson, mm.CircuitPolicyJson);
                        // Replace parameters
                        var existingParams = await _db.WebServiceParams.Where(p => p.MethodId == existingMethod.Id).ToListAsync(ct);
                        _db.WebServiceParams.RemoveRange(existingParams);
                        foreach (var p in mm.Parameters)
                        {
                            var locParsed = Enum.TryParse<ParameterLocation>(p.Location, true, out var loc) ? loc : ParameterLocation.Query;
                            var prm = new WebServiceParam(existingMethod.Id, p.Name, loc, p.Type, p.IsRequired, p.DefaultValue);
                            _db.WebServiceParams.Add(prm);
                        }
                        changes.Add(new { Action = "UpdatedMethod", Service = svc.Name, Path = mm.Path, HttpMethod = mm.HttpMethod });
                    }
                }

                if (updated)
                {
                    await _db.SaveChangesAsync(ct);
                    changes.Add(new { Action = "UpdatedService", Service = svc.Name });
                }
            }
        }

        // Emit Outbox event summarizing registry changes
        var correlationId = HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) && Guid.TryParse(cid, out var corr) ? corr : Guid.NewGuid();
        var tenantId = HttpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tid) ? tid.ToString() : "default";
        var content = JsonSerializer.Serialize(new { Version = "v1", Changes = changes, ImportedAt = DateTime.UtcNow, TenantId = tenantId });
        await _outbox.AddMessageAsync(new OutboxMessage("ApiManager.RegistryImported.v1", content, correlationId, tenantId: tenantId), ct);

        return Ok(new { Imported = true, Changes = changes.Count });
    }
}