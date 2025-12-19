using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class WaitForEventStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<WaitForEventStepHandler> _logger;

    public WaitForEventStepHandler(ILogger<WaitForEventStepHandler> logger)
    {
        _logger = logger;
    }

    public string StepType => "WaitForEvent";

    public Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, StepDsl step, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Workflow {RunId} waiting for event in step {StepId}", run.Id, step.Id);
        
        // In a real implementation, we might register a correlation ID with the event bus here.
        // For MVP, we simply pause the workflow. The external system is responsible for 
        // receiving the event and calling ResumeAsync with the matching WorkflowRunId.
        
        return Task.FromResult(StepExecutionResult.Pause());
    }
}
