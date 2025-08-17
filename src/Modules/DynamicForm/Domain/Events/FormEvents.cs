using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a new form is created.
    /// </summary>
    public class FormCreatedEvent : DomainEvent
    {
        public Guid FormId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string CreatedBy { get; }

        public FormCreatedEvent(Guid formId, string name, string tenantId, string createdBy)
        {
            FormId = formId;
            Name = name;
            TenantId = tenantId;
            CreatedBy = createdBy;
        }
    }

    /// <summary>
    /// Event raised when a form is updated.
    /// </summary>
    public class FormUpdatedEvent : DomainEvent
    {
        public Guid FormId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string UpdatedBy { get; }

        public FormUpdatedEvent(Guid formId, string name, string tenantId, string updatedBy)
        {
            FormId = formId;
            Name = name;
            TenantId = tenantId;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when a form is published.
    /// </summary>
    public class FormPublishedEvent : DomainEvent
    {
        public Guid FormId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public int Version { get; }
        public string PublishedBy { get; }

        public FormPublishedEvent(Guid formId, string name, string tenantId, int version, string publishedBy)
        {
            FormId = formId;
            Name = name;
            TenantId = tenantId;
            Version = version;
            PublishedBy = publishedBy;
        }
    }

    /// <summary>
    /// Event raised when a form is unpublished.
    /// </summary>
    public class FormUnpublishedEvent : DomainEvent
    {
        public Guid FormId { get; }
        public string Name { get; }
        public string TenantId { get; }
        public string UnpublishedBy { get; }

        public FormUnpublishedEvent(Guid formId, string name, string tenantId, string unpublishedBy)
        {
            FormId = formId;
            Name = name;
            TenantId = tenantId;
            UnpublishedBy = unpublishedBy;
        }
    }



    /// <summary>
    /// Event raised when a field is added to a form.
    /// </summary>
    public class FormFieldAddedEvent : DomainEvent
    {
        public Guid FormId { get; }
        public Guid FieldId { get; }
        public string FieldName { get; }
        public string FieldType { get; }
        public string TenantId { get; }
        public string AddedBy { get; }

        public FormFieldAddedEvent(Guid formId, Guid fieldId, string fieldName, string fieldType, string tenantId, string addedBy)
        {
            FormId = formId;
            FieldId = fieldId;
            FieldName = fieldName;
            FieldType = fieldType;
            TenantId = tenantId;
            AddedBy = addedBy;
        }
    }

    /// <summary>
    /// Event raised when a submission is added to a form.
    /// </summary>
    public class FormSubmissionAddedEvent : DomainEvent
    {
        public Guid FormId { get; }
        public Guid SubmissionId { get; }
        public string TenantId { get; }
        public Guid? UserId { get; }
        public string AddedBy { get; }

        public FormSubmissionAddedEvent(Guid formId, Guid submissionId, string tenantId, Guid? userId, string addedBy)
        {
            FormId = formId;
            SubmissionId = submissionId;
            TenantId = tenantId;
            UserId = userId;
            AddedBy = addedBy;
        }
    }

    /// <summary>
    /// Event raised when a form step is created.
    /// </summary>
    public class FormStepCreatedEvent : DomainEvent
    {
        public Guid StepId { get; }
        public Guid FormId { get; }
        public int StepNumber { get; }
        public string Title { get; }
        public string CreatedBy { get; }

        public FormStepCreatedEvent(Guid stepId, Guid formId, int stepNumber, string title, string createdBy)
        {
            StepId = stepId;
            FormId = formId;
            StepNumber = stepNumber;
            Title = title;
            CreatedBy = createdBy;
        }
    }

    /// <summary>
    /// Event raised when a form step is updated.
    /// </summary>
    public class FormStepUpdatedEvent : DomainEvent
    {
        public Guid StepId { get; }
        public Guid FormId { get; }
        public int StepNumber { get; }
        public string Title { get; }
        public string UpdatedBy { get; }

        public FormStepUpdatedEvent(Guid stepId, Guid formId, int stepNumber, string title, string updatedBy)
        {
            StepId = stepId;
            FormId = formId;
            StepNumber = stepNumber;
            Title = title;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when a form step is reordered.
    /// </summary>
    public class FormStepReorderedEvent : DomainEvent
    {
        public Guid StepId { get; }
        public Guid FormId { get; }
        public int OldStepNumber { get; }
        public int NewStepNumber { get; }
        public string ReorderedBy { get; }

        public FormStepReorderedEvent(Guid stepId, Guid formId, int oldStepNumber, int newStepNumber, string reorderedBy)
        {
            StepId = stepId;
            FormId = formId;
            OldStepNumber = oldStepNumber;
            NewStepNumber = newStepNumber;
            ReorderedBy = reorderedBy;
        }
    }
}