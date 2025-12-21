using CoreAxis.EventBus;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class HumanTaskStepHandler : IWorkflowStepHandler
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<HumanTaskStepHandler> _logger;

    public HumanTaskStepHandler(IEventBus eventBus, ILogger<HumanTaskStepHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public string StepType => "HumanTaskStep";

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Requesting Human Task for workflow {RunId}, step {StepId}", run.Id, step.Id);

        // Extract assignee info from step configuration
        // Assuming step.Configuration has "assigneeType" and "assigneeId"
        // In a real implementation, we might evaluate expressions here to determine assignee
        
        string assigneeType = "Role"; // Default
        string assigneeId = "Admin";  // Default
        string? payloadJson = null;
        string? allowedActionsJson = null;
        DateTime? dueAt = null;

        if (step.Config != null)
        {
            if (step.Config.TryGetValue("assigneeType", out var typeObj))
                assigneeType = typeObj?.ToString() ?? "Role";
            
            if (step.Config.TryGetValue("assigneeId", out var idObj))
                assigneeId = idObj?.ToString() ?? "Admin";

            // Extract other properties if needed
            payloadJson = JsonSerializer.Serialize(step.Config);
        }

        // Publish integration event to request task creation
        var correlationId = Guid.TryParse(run.CorrelationId, out var cid) ? cid : Guid.NewGuid();
        
        var evt = new HumanTaskRequested(
            run.Id,
            step.Id,
            assigneeType,
            assigneeId,
            payloadJson,
            allowedActionsJson,
            dueAt,
            correlationId
        );

        await _eventBus.PublishAsync(evt);

        // Pause for human interaction
        return StepExecutionResult.Pause();
    }
}
