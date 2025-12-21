using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public interface IWorkflowStepHandler
{
    string StepType { get; }
    Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken);
}

public class StepExecutionResult
{
    public bool IsSuccess { get; set; }
    public bool IsPaused { get; set; }
    public string? NextStepId { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object>? OutputContext { get; set; }

    public static StepExecutionResult Success(string? nextStepId = null, Dictionary<string, object>? output = null)
    {
        return new StepExecutionResult { IsSuccess = true, NextStepId = nextStepId, OutputContext = output };
    }

    public static StepExecutionResult Pause()
    {
        return new StepExecutionResult { IsSuccess = true, IsPaused = true };
    }

    public static StepExecutionResult Failure(string error)
    {
        return new StepExecutionResult { IsSuccess = false, Error = error };
    }
}
