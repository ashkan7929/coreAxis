using CoreAxis.SharedKernel.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Interface for managing form events.
    /// </summary>
    public interface IFormEventManager
    {
        /// <summary>
        /// Registers a form event handler for a specific form.
        /// </summary>
        /// <param name="formId">The form ID.</param>
        /// <param name="handler">The event handler.</param>
        void RegisterHandler(Guid formId, IFormEventHandler handler);

        /// <summary>
        /// Registers a global form event handler for all forms.
        /// </summary>
        /// <param name="handler">The event handler.</param>
        void RegisterGlobalHandler(IFormEventHandler handler);

        /// <summary>
        /// Unregisters a form event handler for a specific form.
        /// </summary>
        /// <param name="formId">The form ID.</param>
        /// <param name="handler">The event handler.</param>
        void UnregisterHandler(Guid formId, IFormEventHandler handler);

        /// <summary>
        /// Unregisters a global form event handler.
        /// </summary>
        /// <param name="handler">The event handler.</param>
        void UnregisterGlobalHandler(IFormEventHandler handler);

        /// <summary>
        /// Triggers the form initialization event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the aggregated event results.</returns>
        Task<Result<List<FormEventResult>>> TriggerOnInitAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers the form field change event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the aggregated event results.</returns>
        Task<Result<List<FormEventResult>>> TriggerOnChangeAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers the before form submission event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the aggregated event results.</returns>
        Task<Result<List<FormEventResult>>> TriggerBeforeSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers the after form submission event.
        /// </summary>
        /// <param name="context">The form event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the aggregated event results.</returns>
        Task<Result<List<FormEventResult>>> TriggerAfterSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all registered handlers for a specific form.
        /// </summary>
        /// <param name="formId">The form ID.</param>
        /// <returns>A list of registered handlers.</returns>
        IReadOnlyList<IFormEventHandler> GetHandlers(Guid formId);

        /// <summary>
        /// Gets all global handlers.
        /// </summary>
        /// <returns>A list of global handlers.</returns>
        IReadOnlyList<IFormEventHandler> GetGlobalHandlers();
    }
}