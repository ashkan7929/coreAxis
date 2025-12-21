using CoreAxis.Modules.Workflow.Application.Services.Compensation;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class CompensationStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<CompensationStepHandler> _logger;
    private readonly ICompensationExecutor _compensationExecutor;

    public CompensationStepHandler(ILogger<CompensationStepHandler> logger, ICompensationExecutor compensationExecutor)
    {
        _logger = logger;
        _compensationExecutor = compensationExecutor;
    }

    public string StepType => "CompensationStep";

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CompensationStep executing for workflow {RunId}", run.Id);

        // Trigger compensation
        // Note: This will execute compensation for all previously completed steps that have compensation configured.
        await _compensationExecutor.CompensateAsync(run, cancellationToken);
        
        string? nextStepId = step.Transitions?.FirstOrDefault()?.To;
        return StepExecutionResult.Success(nextStepId);
    }
}
