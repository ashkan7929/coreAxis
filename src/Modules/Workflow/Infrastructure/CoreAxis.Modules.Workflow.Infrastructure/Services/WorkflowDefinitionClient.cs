using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.SharedKernel.Versioning;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Infrastructure.Services;

public class WorkflowDefinitionClient : IWorkflowDefinitionClient
{
    private readonly WorkflowDbContext _context;

    public WorkflowDefinitionClient(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> WorkflowDefinitionExistsAsync(string definitionCode, int? version = null, CancellationToken cancellationToken = default)
    {
        var def = await _context.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Code == definitionCode, cancellationToken);
        if (def == null) return false;

        if (version.HasValue)
        {
            return await _context.WorkflowDefinitionVersions.AnyAsync(v => v.WorkflowDefinitionId == def.Id && v.VersionNumber == version.Value, cancellationToken);
        }

        return true;
    }

    public async Task<bool> IsWorkflowDefinitionPublishedAsync(string definitionCode, int version, CancellationToken cancellationToken = default)
    {
        var def = await _context.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Code == definitionCode, cancellationToken);
        if (def == null) return false;

        return await _context.WorkflowDefinitionVersions.AnyAsync(v => 
            v.WorkflowDefinitionId == def.Id && 
            v.VersionNumber == version && 
            v.Status == VersionStatus.Published, 
            cancellationToken);
    }
}
