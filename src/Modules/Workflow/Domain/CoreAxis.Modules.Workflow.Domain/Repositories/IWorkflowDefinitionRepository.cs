using CoreAxis.Modules.Workflow.Domain.Entities;

namespace CoreAxis.Modules.Workflow.Domain.Repositories;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinitionVersion?> GetVersionAsync(string code, int version, CancellationToken ct);
}
