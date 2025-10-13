using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services;

public class WorkflowAdminService : IWorkflowAdminService
{
    private readonly WorkflowDbContext _db;

    public WorkflowAdminService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsAsync(CancellationToken ct = default)
    {
        return await _db.WorkflowDefinitions
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<WorkflowDefinition> CreateDefinitionAsync(string code, string name, string? description, string createdBy, CancellationToken ct = default)
    {
        var def = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkflowDefinitions.Add(def);
        await _db.SaveChangesAsync(ct);
        return def;
    }

    public async Task<WorkflowDefinitionVersion> CreateVersionAsync(Guid workflowId, int versionNumber, string dslJson, string? changelog, string createdBy, CancellationToken ct = default)
    {
        var ver = new WorkflowDefinitionVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            VersionNumber = versionNumber,
            IsPublished = false,
            DslJson = dslJson,
            SchemaVersion = 1,
            Changelog = changelog,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        _db.WorkflowDefinitionVersions.Add(ver);
        await _db.SaveChangesAsync(ct);
        return ver;
    }

    public async Task<bool> PublishVersionAsync(Guid workflowId, int versionNumber, CancellationToken ct = default)
    {
        var ver = await _db.WorkflowDefinitionVersions
            .FirstOrDefaultAsync(v => v.WorkflowDefinitionId == workflowId && v.VersionNumber == versionNumber, ct);
        if (ver == null) return false;
        ver.IsPublished = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UnpublishVersionAsync(Guid workflowId, int versionNumber, CancellationToken ct = default)
    {
        var ver = await _db.WorkflowDefinitionVersions
            .FirstOrDefaultAsync(v => v.WorkflowDefinitionId == workflowId && v.VersionNumber == versionNumber, ct);
        if (ver == null) return false;
        ver.IsPublished = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<object> DryRunAsync(Guid workflowId, int versionNumber, string inputContextJson, CancellationToken ct = default)
    {
        var ver = await _db.WorkflowDefinitionVersions
            .FirstOrDefaultAsync(v => v.WorkflowDefinitionId == workflowId && v.VersionNumber == versionNumber, ct);
        if (ver == null)
        {
            return new { ok = false, error = "Workflow version not found" };
        }

        // Basic validation of DSL JSON structure
        try
        {
            using var doc = JsonDocument.Parse(ver.DslJson);
            var root = doc.RootElement;
            var hasSteps = root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array;
            var inputsOk = root.TryGetProperty("inputs", out _);

            using var inputDoc = JsonDocument.Parse(inputContextJson);

            var summary = new
            {
                ok = hasSteps && inputsOk,
                stepsCount = hasSteps ? steps.GetArrayLength() : 0,
                usedInputs = inputsOk ? root.GetProperty("inputs").ToString() : "",
                sampleOutput = new
                {
                    policyId = "dryrun-policy-id",
                    fundBalance = 1000,
                    walletDepositTxId = "dryrun-wallet-tx",
                    commissions = new { approved = true, paid = true }
                }
            };
            return summary;
        }
        catch (Exception ex)
        {
            return new { ok = false, error = ex.Message };
        }
    }
}