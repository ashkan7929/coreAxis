using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class EndStepHandler : IWorkflowStepHandler
{
    public string StepType => "EndStep";

    public Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        // EndStep simply completes successfully with no next step, 
        // which signals the executor to complete the workflow run.
        return Task.FromResult(StepExecutionResult.Success(nextStepId: null));
    }
}
