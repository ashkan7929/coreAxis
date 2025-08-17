using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a new form field is created.
    /// </summary>
    public class FormFieldCreatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public string FieldType { get; }
        public bool IsRequired { get; }
        public int Order { get; }
        public string CreatedBy { get; }

        public FormFieldCreatedEvent(Guid fieldId, Guid formId, string name, string fieldType, bool isRequired, int order, string createdBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            FieldType = fieldType;
            IsRequired = isRequired;
            Order = order;
            CreatedBy = createdBy;
        }
    }

    /// <summary>
    /// Event raised when a form field is updated.
    /// </summary>
    public class FormFieldUpdatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public string Label { get; }
        public string FieldType { get; }
        public bool IsRequired { get; }
        public string UpdatedBy { get; }

        public FormFieldUpdatedEvent(Guid fieldId, Guid formId, string name, string label, string fieldType, bool isRequired, string updatedBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            Label = label;
            FieldType = fieldType;
            IsRequired = isRequired;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when form field validation rules are updated.
    /// </summary>
    public class FormFieldValidationUpdatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public string ValidationRules { get; }
        public string UpdatedBy { get; }

        public FormFieldValidationUpdatedEvent(Guid fieldId, Guid formId, string name, string validationRules, string updatedBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            ValidationRules = validationRules;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when form field options are updated.
    /// </summary>
    public class FormFieldOptionsUpdatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public string Options { get; }
        public string UpdatedBy { get; }

        public FormFieldOptionsUpdatedEvent(Guid fieldId, Guid formId, string name, string options, string updatedBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            Options = options;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when form field conditional logic is updated.
    /// </summary>
    public class FormFieldConditionalLogicUpdatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public string ConditionalLogic { get; }
        public string UpdatedBy { get; }

        public FormFieldConditionalLogicUpdatedEvent(Guid fieldId, Guid formId, string name, string conditionalLogic, string updatedBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            ConditionalLogic = conditionalLogic;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when form field calculation expression is updated.
    /// </summary>
    public class FormFieldCalculationUpdatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public string CalculationExpression { get; }
        public bool IsCalculated { get; }
        public string UpdatedBy { get; }

        public FormFieldCalculationUpdatedEvent(Guid fieldId, Guid formId, string name, string calculationExpression, bool isCalculated, string updatedBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            CalculationExpression = calculationExpression;
            IsCalculated = isCalculated;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when form field order is updated.
    /// </summary>
    public class FormFieldOrderUpdatedEvent : DomainEvent
    {
        public Guid FieldId { get; }
        public Guid FormId { get; }
        public string Name { get; }
        public int OldOrder { get; }
        public int NewOrder { get; }
        public string UpdatedBy { get; }

        public FormFieldOrderUpdatedEvent(Guid fieldId, Guid formId, string name, int oldOrder, int newOrder, string updatedBy)
        {
            FieldId = fieldId;
            FormId = formId;
            Name = name;
            OldOrder = oldOrder;
            NewOrder = newOrder;
            UpdatedBy = updatedBy;
        }
    }
}