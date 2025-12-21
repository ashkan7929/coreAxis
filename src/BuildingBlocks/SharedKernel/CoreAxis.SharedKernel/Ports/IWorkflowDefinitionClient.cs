using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel.Ports;

public interface IWorkflowDefinitionClient
{
    Task<bool> WorkflowDefinitionExistsAsync(string definitionCode, int? version = null, CancellationToken cancellationToken = default);
    Task<bool> IsWorkflowDefinitionPublishedAsync(string definitionCode, int version, CancellationToken cancellationToken = default);
}
