using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class CalculationStepHandler : IWorkflowStepHandler
{
    public string StepType => "CalculationStep";

    public Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        // Placeholder calculation
        var output = new Dictionary<string, object>();
        if (step.Config != null && step.Config.TryGetValue("outputVariable", out var outVar))
        {
            output[outVar.ToString()!] = 42; // The answer to everything
        }

        string? nextStepId = step.Transitions?.FirstOrDefault()?.To;
        return Task.FromResult(StepExecutionResult.Success(nextStepId, output));
    }
}
