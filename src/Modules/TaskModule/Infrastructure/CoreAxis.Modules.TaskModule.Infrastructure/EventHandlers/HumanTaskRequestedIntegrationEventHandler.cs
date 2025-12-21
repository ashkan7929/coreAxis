using CoreAxis.EventBus;
using CoreAxis.Modules.TaskModule.Domain.Entities;
using CoreAxis.Modules.TaskModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.TaskModule.Infrastructure.EventHandlers;

public class HumanTaskRequestedIntegrationEventHandler : IIntegrationEventHandler<HumanTaskRequested>
{
    private readonly TaskDbContext _context;
    private readonly ILogger<HumanTaskRequestedIntegrationEventHandler> _logger;

    public HumanTaskRequestedIntegrationEventHandler(TaskDbContext context, ILogger<HumanTaskRequestedIntegrationEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(HumanTaskRequested @event)
    {
        _logger.LogInformation("Creating Human Task for Workflow {RunId}, Step {StepId}", @event.WorkflowRunId, @event.StepId);

        var task = new TaskInstance
        {
            WorkflowId = @event.WorkflowRunId,
            StepKey = @event.StepId,
            Status = "Open",
            AssigneeType = @event.AssigneeType,
            AssigneeId = @event.AssigneeId,
            PayloadJson = @event.PayloadJson,
            AllowedActionsJson = @event.AllowedActionsJson,
            DueAt = @event.DueAt,
            CreatedBy = "System",
            LastModifiedBy = "System"
        };

        _context.TaskInstances.Add(task);
        await _context.SaveChangesAsync();
    }
}
