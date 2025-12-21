using CoreAxis.Modules.Workflow.Domain.Entities;

namespace CoreAxis.Modules.Workflow.Application.Services.Compensation;

public interface ICompensationExecutor
{
    Task CompensateAsync(WorkflowRun run, CancellationToken cancellationToken);
}
