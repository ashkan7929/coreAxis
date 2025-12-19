using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.Modules.Workflow.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.DomainEvents;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.Workflow.Application.EventHandlers;

public class WorkflowRunStartedDomainEventHandler : IDomainEventHandler<WorkflowRunStartedDomainEvent>
{
    private readonly IWorkflowExecutor _executor;
    private readonly ILogger<WorkflowRunStartedDomainEventHandler> _logger;

    public WorkflowRunStartedDomainEventHandler(IWorkflowExecutor executor, ILogger<WorkflowRunStartedDomainEventHandler> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task HandleAsync(WorkflowRunStartedDomainEvent domainEvent)
    {
        _logger.LogInformation("Handling WorkflowRunStartedDomainEvent for run {RunId}", domainEvent.WorkflowRunId);
        await _executor.ExecuteStepAsync(domainEvent.WorkflowRunId, "start");
    }
}
