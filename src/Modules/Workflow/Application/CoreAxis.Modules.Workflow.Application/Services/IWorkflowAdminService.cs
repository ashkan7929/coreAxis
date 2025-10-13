using CoreAxis.Modules.Workflow.Domain.Entities;

namespace CoreAxis.Modules.Workflow.Application.Services;

public interface IWorkflowAdminService
{
    Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsAsync(CancellationToken ct = default);
    Task<WorkflowDefinition> CreateDefinitionAsync(string code, string name, string? description, string createdBy, CancellationToken ct = default);
    Task<WorkflowDefinitionVersion> CreateVersionAsync(Guid workflowId, int versionNumber, string dslJson, string? changelog, string createdBy, CancellationToken ct = default);
    Task<bool> PublishVersionAsync(Guid workflowId, int versionNumber, CancellationToken ct = default);
    Task<bool> UnpublishVersionAsync(Guid workflowId, int versionNumber, CancellationToken ct = default);
    Task<object> DryRunAsync(Guid workflowId, int versionNumber, string inputContextJson, CancellationToken ct = default);
}