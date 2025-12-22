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

        Guid? workflowRunId = @event.WorkflowRunId;

        // Fallback to metadata parsing if WorkflowRunId is missing
        if (!workflowRunId.HasValue && !string.IsNullOrEmpty(@event.Metadata))
        {
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
        }

        if (workflowRunId.HasValue)
        {
            var input = new Dictionary<string, object>
            {
                ["submissionId"] = @event.SubmissionId,
                ["submissionData"] = @event.SubmissionData,
                ["userId"] = @event.UserId
            };

            if (!string.IsNullOrEmpty(@event.StepKey))
            {
                input["stepKey"] = @event.StepKey;
            }

            try
            {
                await _workflowExecutor.ResumeAsync(workflowRunId.Value, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming workflow {WorkflowRunId} for submission {SubmissionId}", workflowRunId, @event.SubmissionId);
                throw; // Re-throw to let EventBus handle it (or not, if we want to suppress)
            }
        }
        else
        {
            // Try to find by CorrelationId
            if (@event.CorrelationId != Guid.Empty)
            {
                _logger.LogInformation("Attempting to resume workflow by CorrelationId {CorrelationId}", @event.CorrelationId);
                
                var input = new Dictionary<string, object>
                {
                    ["submissionId"] = @event.SubmissionId,
                    ["submissionData"] = @event.SubmissionData,
                    ["userId"] = @event.UserId
                };

                if (!string.IsNullOrEmpty(@event.StepKey))
                {
                    input["stepKey"] = @event.StepKey;
                }

                try
                {
                    await _workflowExecutor.SignalByCorrelationAsync(@event.CorrelationId.ToString(), "FormSubmitted", input);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error signaling workflow by correlation {CorrelationId} for submission {SubmissionId}", @event.CorrelationId, @event.SubmissionId);
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("Could not find workflowRunId (explicit or in metadata) or CorrelationId for Submission {SubmissionId}", @event.SubmissionId);
            }
        }
    }
}
