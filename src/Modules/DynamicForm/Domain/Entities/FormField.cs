using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a field within a dynamic form.
    /// </summary>
    public class FormField : EntityBase
    {
        /// <summary>
        /// Gets or sets the form identifier that this field belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the name/key of the field.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display label of the field.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the type of the field (text, number, email, select, etc.).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field is required.
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets the default value of the field.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text for the field.
        /// </summary>
        [MaxLength(200)]
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the help text for the field.
        /// </summary>
        [MaxLength(500)]
        public string HelpText { get; set; }

        /// <summary>
        /// Gets or sets the validation rules as JSON.
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets the options for select/radio/checkbox fields as JSON.
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// Gets or sets the conditional logic for showing/hiding this field as JSON.
        /// </summary>
        public string ConditionalLogic { get; set; }

        /// <summary>
        /// Gets or sets the order/position of this field in the form.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the CSS classes for styling the field.
        /// </summary>
        [MaxLength(200)]
        public string CssClasses { get; set; }

        /// <summary>
        /// Gets or sets additional attributes as JSON.
        /// </summary>
        public string Attributes { get; set; }

        /// <summary>
        /// Gets or sets the calculation expression for computed fields.
        /// </summary>
        [MaxLength(1000)]
        public string CalculationExpression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field is calculated/computed.
        /// </summary>
        public bool IsCalculated { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this field is read-only.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the navigation property to the parent form.
        /// </summary>
        public virtual Form Form { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormField"/> class.
        /// </summary>
        protected FormField()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormField"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="fieldType">The field type.</param>
        /// <param name="order">The field order.</param>
        /// <param name="createdBy">The user who created the field.</param>
        public FormField(Guid formId, string name, string label, string fieldType, int order, string createdBy)
        {
            if (formId == Guid.Empty)
                throw new ArgumentException("Form ID cannot be empty.", nameof(formId));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Field label cannot be null or empty.", nameof(label));
            if (string.IsNullOrWhiteSpace(fieldType))
                throw new ArgumentException("Field type cannot be null or empty.", nameof(fieldType));

            Id = Guid.NewGuid();
            FormId = formId;
            Name = name;
            Label = label;
            FieldType = fieldType;
            Order = order;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormFieldCreatedEvent(Id, FormId, Name, FieldType, IsRequired, Order, CreatedBy));
        }

        /// <summary>
        /// Updates the field with new information.
        /// </summary>
        /// <param name="label">The new label.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="placeholder">The placeholder text.</param>
        /// <param name="helpText">The help text.</param>
        /// <param name="modifiedBy">The user who modified the field.</param>
        public void Update(string label, bool isRequired, string defaultValue, string placeholder, string helpText, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Field label cannot be null or empty.", nameof(label));

            Label = label;
            IsRequired = isRequired;
            DefaultValue = defaultValue;
            Placeholder = placeholder;
            HelpText = helpText;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormFieldUpdatedEvent(Id, FormId, Name, Label, FieldType, IsRequired, modifiedBy));
        }

        /// <summary>
        /// Sets the validation rules for the field.
        /// </summary>
        /// <param name="validationRules">The validation rules as JSON.</param>
        /// <param name="modifiedBy">The user who set the validation rules.</param>
        public void SetValidationRules(string validationRules, string modifiedBy)
        {
            ValidationRules = validationRules;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormFieldValidationUpdatedEvent(Id, FormId, Name, validationRules, modifiedBy));
        }

        /// <summary>
        /// Sets the options for select/radio/checkbox fields.
        /// </summary>
        /// <param name="options">The options as JSON.</param>
        /// <param name="modifiedBy">The user who set the options.</param>
        public void SetOptions(string options, string modifiedBy)
        {
            Options = options;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormFieldOptionsUpdatedEvent(Id, FormId, Name, options, modifiedBy));
        }

        /// <summary>
        /// Sets the conditional logic for the field.
        /// </summary>
        /// <param name="conditionalLogic">The conditional logic as JSON.</param>
        /// <param name="modifiedBy">The user who set the conditional logic.</param>
        public void SetConditionalLogic(string conditionalLogic, string modifiedBy)
        {
            ConditionalLogic = conditionalLogic;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormFieldConditionalLogicUpdatedEvent(Id, FormId, Name, conditionalLogic, modifiedBy));
        }

        /// <summary>
        /// Sets the calculation expression for computed fields.
        /// </summary>
        /// <param name="calculationExpression">The calculation expression.</param>
        /// <param name="modifiedBy">The user who set the calculation expression.</param>
        public void SetCalculationExpression(string calculationExpression, string modifiedBy)
        {
            CalculationExpression = calculationExpression;
            IsCalculated = !string.IsNullOrWhiteSpace(calculationExpression);
            IsReadOnly = IsCalculated; // Calculated fields are typically read-only
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormFieldCalculationUpdatedEvent(Id, FormId, Name, calculationExpression, IsCalculated, modifiedBy));
        }

        /// <summary>
        /// Updates the field order.
        /// </summary>
        /// <param name="newOrder">The new order.</param>
        /// <param name="modifiedBy">The user who updated the order.</param>
        public void UpdateOrder(int newOrder, string modifiedBy)
        {
            if (newOrder < 0)
                throw new ArgumentException("Order cannot be negative.", nameof(newOrder));

            var oldOrder = Order;
            Order = newOrder;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormFieldOrderUpdatedEvent(Id, FormId, Name, oldOrder, newOrder, modifiedBy));
        }
    }


}