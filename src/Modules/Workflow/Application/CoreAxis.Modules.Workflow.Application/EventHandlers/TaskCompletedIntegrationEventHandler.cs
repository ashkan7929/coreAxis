using CoreAxis.EventBus;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.Workflow.Application.EventHandlers;

public class TaskCompletedIntegrationEventHandler : IIntegrationEventHandler<HumanTaskCompleted>
{
    private readonly IWorkflowExecutor _executor;
    private readonly ILogger<TaskCompletedIntegrationEventHandler> _logger;

    public TaskCompletedIntegrationEventHandler(IWorkflowExecutor executor, ILogger<TaskCompletedIntegrationEventHandler> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task HandleAsync(HumanTaskCompleted @event)
    {
        _logger.LogInformation("Handling HumanTaskCompleted for Workflow {RunId}, Task {TaskId}, Outcome {Outcome}", @event.WorkflowRunId, @event.TaskId, @event.Outcome);

        var payload = new Dictionary<string, object>
        {
            { "taskId", @event.TaskId },
            { "outcome", @event.Outcome },
            { "comment", @event.Comment ?? string.Empty }
        };

        if (!string.IsNullOrEmpty(@event.PayloadJson))
        {
            payload.Add("data", System.Text.Json.JsonSerializer.Deserialize<object>(@event.PayloadJson) ?? new object());
        }

        // Resume workflow with signal
        // We use "TaskCompleted" as the signal name, or maybe just Resume?
        // Since HumanTaskStep pauses and waits, we can just Resume it with context.
        // But IWorkflowExecutor.ResumeAsync takes input.
        
        await _executor.ResumeAsync(@event.WorkflowRunId, payload);
    }
}
