using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class FormStepHandler : IWorkflowStepHandler
{
    public string StepType => "FormStep";

    public Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        // Form step always pauses execution to wait for user input
        return Task.FromResult(StepExecutionResult.Pause());
    }
}
