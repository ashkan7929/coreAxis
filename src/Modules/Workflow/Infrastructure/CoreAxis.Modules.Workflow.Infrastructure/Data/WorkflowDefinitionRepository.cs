using CoreAxis.Modules.Workflow.Domain.Repositories;
using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Infrastructure.Data;

public class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly WorkflowDbContext _context;

    public WorkflowDefinitionRepository(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowDefinitionVersion?> GetVersionAsync(string code, int version, CancellationToken ct)
    {
        return await _context.WorkflowDefinitionVersions
            .Include(v => v.WorkflowDefinition)
            .FirstOrDefaultAsync(v => v.WorkflowDefinition != null && v.WorkflowDefinition.Code == code && v.VersionNumber == version, ct);
    }
}
