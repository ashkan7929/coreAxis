using CoreAxis.EventBus;
using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.EventHandlers;

public class FormSubmittedIntegrationEventHandler : IIntegrationEventHandler<FormSubmitted>
{
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ILogger<FormSubmittedIntegrationEventHandler> _logger;

    public FormSubmittedIntegrationEventHandler(
        IWorkflowExecutor workflowExecutor,
        ILogger<FormSubmittedIntegrationEventHandler> logger)
    {
        _workflowExecutor = workflowExecutor;
        _logger = logger;
    }

    public async Task HandleAsync(FormSubmitted @event)
    {
        _logger.LogInformation("Handling FormSubmitted event for Submission {SubmissionId}", @event.SubmissionId);

        if (string.IsNullOrEmpty(@event.Metadata))
        {
            _logger.LogWarning("No metadata in FormSubmitted event. Cannot resume workflow.");
            return;
        }

        Guid? workflowRunId = null;

        try
        {
            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(@event.Metadata);
            if (metadata != null && metadata.TryGetValue("workflowRunId", out var runIdObj))
            {
                if (Guid.TryParse(runIdObj.ToString(), out var id))
                {
                    workflowRunId = id;
                }
            }
        }
        catch
        {
            // Ignore JSON error, try plain ID
            if (Guid.TryParse(@event.Metadata, out var id))
            {
                workflowRunId = id;
            }
        }

        if (workflowRunId.HasValue)
        {
            var input = new Dictionary<string, object>
            {
                ["submissionId"] = @event.SubmissionId,
                ["submissionData"] = @event.SubmissionData,
                ["userId"] = @event.UserId
            };

            await _workflowExecutor.ResumeAsync(workflowRunId.Value, input);
        }
        else
        {
            _logger.LogWarning("Could not find workflowRunId in metadata for Submission {SubmissionId}", @event.SubmissionId);
        }
    }
}
