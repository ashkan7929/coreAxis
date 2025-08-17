using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.Forms
{
    /// <summary>
    /// Command to trigger a form event.
    /// </summary>
    public class TriggerFormEventCommand : IRequest<Result<List<FormEventResult>>>
    {
        /// <summary>
        /// Gets or sets the form ID.
        /// </summary>
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public FormEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the user ID who triggered the event.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current form data.
        /// </summary>
        public Dictionary<string, object?> FormData { get; set; } = new();

        /// <summary>
        /// Gets or sets the previous form data (for onChange events).
        /// </summary>
        public Dictionary<string, object?> PreviousFormData { get; set; } = new();

        /// <summary>
        /// Gets or sets the changed field name (for onChange events).
        /// </summary>
        public string? ChangedField { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the event.
        /// </summary>
        public Dictionary<string, object?> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents the type of form event.
    /// </summary>
    public enum FormEventType
    {
        /// <summary>
        /// Form initialization event.
        /// </summary>
        OnInit,

        /// <summary>
        /// Form field change event.
        /// </summary>
        OnChange,

        /// <summary>
        /// Before form submission event.
        /// </summary>
        BeforeSubmit,

        /// <summary>
        /// After form submission event.
        /// </summary>
        AfterSubmit
    }
}