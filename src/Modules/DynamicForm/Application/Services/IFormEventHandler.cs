using CoreAxis.Modules.DynamicForm.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Represents the context for form events.
    /// </summary>
    public class FormEventContext
    {
        /// <summary>
        /// Gets or sets the form ID.
        /// </summary>
        public Guid FormId { get; set; }

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

        /// <summary>
        /// Gets or sets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether the event should be cancelled.
        /// Only applicable for beforeSubmit events.
        /// </summary>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// Gets or sets the cancellation reason.
        /// </summary>
        public string? CancellationReason { get; set; }
    }

    /// <summary>
    /// Represents the result of a form event handler.
    /// </summary>
    public class FormEventResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the event was handled successfully.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the error message if the event handling failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets additional data to be returned from the event handler.
        /// </summary>
        public Dictionary<string, object?> Data { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the form data should be updated.
        /// </summary>
        public bool UpdateFormData { get; set; } = false;

        /// <summary>
        /// Gets or sets the updated form data.
        /// </summary>
        public Dictionary<string, object?> UpdatedFormData { get; set; } = new();
    }

    /// <summary>
    /// Interface for form event handlers.
    /// </summary>
    public interface IFormEventHandler
    {
        /// <summary>
        /// Handles the form initialization event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<FormEventResult> OnInitAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles the form field change event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<FormEventResult> OnChangeAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles the before form submission event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<FormEventResult> BeforeSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles the after form submission event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<FormEventResult> AfterSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Base class for form event handlers with default implementations.
    /// </summary>
    public abstract class FormEventHandlerBase : IFormEventHandler
    {
        /// <inheritdoc/>
        public virtual Task<FormEventResult> OnInitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FormEventResult());
        }

        /// <inheritdoc/>
        public virtual Task<FormEventResult> OnChangeAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FormEventResult());
        }

        /// <inheritdoc/>
        public virtual Task<FormEventResult> BeforeSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
        return Task.FromResult(new FormEventResult());
        }

        /// <inheritdoc/>
        public virtual Task<FormEventResult> AfterSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FormEventResult());
        }
    }
}