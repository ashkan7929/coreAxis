using CoreAxis.EventBus;
using CoreAxis.Modules.NotificationModule.Application.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.NotificationModule.Application.EventHandlers;

public class TaskCreatedEventHandler : IIntegrationEventHandler<HumanTaskRequested>
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<TaskCreatedEventHandler> _logger;

    public TaskCreatedEventHandler(NotificationService notificationService, ILogger<TaskCreatedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(HumanTaskRequested @event)
    {
        _logger.LogInformation("Handling HumanTaskRequested for workflow {WorkflowRunId}, assignee {Assignee}", @event.WorkflowRunId, @event.AssigneeId);

        // Assumption: AssigneeId is the recipient (e.g., user ID or email).
        // In a real system, we'd look up the user's email/phone from AuthModule or Identity service.
        // For now, we assume AssigneeId works or we use a placeholder if it's not an email.
        
        var recipient = @event.AssigneeId;
        var parameters = new Dictionary<string, string>
        {
            { "WorkflowRunId", @event.WorkflowRunId.ToString() },
            { "StepId", @event.StepId },
            { "DueDate", @event.DueAt?.ToString("g") ?? "N/A" }
        };

        // We use a default template key for tasks
        await _notificationService.SendNotificationAsync("TASK_ASSIGNED", recipient, parameters);
    }
}
