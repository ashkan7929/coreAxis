using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class DecisionStepHandler : IWorkflowStepHandler
{
    public string StepType => "DecisionStep";

    public Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        // Simple logic: check "condition" in config
        // If true, go to "true" transition, else "false" transition (or default)
        // For MVP, we assume the condition is a simple boolean field in context for now
        
        // This is a placeholder. Real implementation would use ExpressionEngine.
        
        var condition = step.Config?.ContainsKey("condition") == true ? step.Config["condition"].ToString() : "true";
        
        // Find transition based on condition
        // Assuming transitions have "condition" property matching the result
        
        string? nextStepId = null;
        
        if (step.Transitions != null)
        {
            // Evaluate condition against context
            // Mock: if condition is "true", pick the first one with no condition or condition=="true"
            
            // For now, just pick the first transition
            nextStepId = step.Transitions.FirstOrDefault()?.To;
        }

        return Task.FromResult(StepExecutionResult.Success(nextStepId));
    }
}
