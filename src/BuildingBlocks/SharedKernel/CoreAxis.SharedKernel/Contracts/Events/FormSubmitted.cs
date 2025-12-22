using CoreAxis.EventBus;
using System;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class FormSubmitted : IntegrationEvent
{
    public Guid FormId { get; }
    public Guid SubmissionId { get; }
    public Guid UserId { get; }
    public string SubmissionData { get; }
    public string? Metadata { get; }
    public Guid? WorkflowRunId { get; }
    public string? StepKey { get; }

    public FormSubmitted(
        Guid formId, 
        Guid submissionId, 
        Guid userId, 
        string submissionData, 
        string? metadata,
        Guid correlationId,
        Guid? workflowRunId = null,
        string? stepKey = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId)
    {
        FormId = formId;
        SubmissionId = submissionId;
        UserId = userId;
        SubmissionData = submissionData;
        Metadata = metadata;
        WorkflowRunId = workflowRunId;
        StepKey = stepKey;
    }
}
