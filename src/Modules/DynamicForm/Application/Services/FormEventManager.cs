using CoreAxis.SharedKernel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Implementation of form event manager.
    /// </summary>
    public class FormEventManager : IFormEventManager
    {
        private readonly ConcurrentDictionary<Guid, List<IFormEventHandler>> _formHandlers = new();
        private readonly List<IFormEventHandler> _globalHandlers = new();
        private readonly object _lock = new();
        private readonly ILogger<FormEventManager> _logger;

        public FormEventManager(ILogger<FormEventManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void RegisterHandler(Guid formId, IFormEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _formHandlers.AddOrUpdate(formId,
                new List<IFormEventHandler> { handler },
                (key, existingHandlers) =>
                {
                    lock (_lock)
                    {
                        if (!existingHandlers.Contains(handler))
                        {
                            existingHandlers.Add(handler);
                        }
                        return existingHandlers;
                    }
                });

            _logger.LogDebug("Registered form event handler {HandlerType} for form {FormId}", 
                handler.GetType().Name, formId);
        }

        /// <inheritdoc/>
        public void RegisterGlobalHandler(IFormEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                if (!_globalHandlers.Contains(handler))
                {
                    _globalHandlers.Add(handler);
                }
            }

            _logger.LogDebug("Registered global form event handler {HandlerType}", handler.GetType().Name);
        }

        /// <inheritdoc/>
        public void UnregisterHandler(Guid formId, IFormEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_formHandlers.TryGetValue(formId, out var handlers))
            {
                lock (_lock)
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                    {
                        _formHandlers.TryRemove(formId, out _);
                    }
                }

                _logger.LogDebug("Unregistered form event handler {HandlerType} for form {FormId}", 
                    handler.GetType().Name, formId);
            }
        }

        /// <inheritdoc/>
        public void UnregisterGlobalHandler(IFormEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                _globalHandlers.Remove(handler);
            }

            _logger.LogDebug("Unregistered global form event handler {HandlerType}", handler.GetType().Name);
        }

        /// <inheritdoc/>
        public async Task<Result<List<FormEventResult>>> TriggerOnInitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return await ExecuteHandlersAsync(context, 
                (handler, ctx, ct) => handler.OnInitAsync(ctx, ct), 
                "OnInit", cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<List<FormEventResult>>> TriggerOnChangeAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return await ExecuteHandlersAsync(context, 
                (handler, ctx, ct) => handler.OnChangeAsync(ctx, ct), 
                "OnChange", cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<List<FormEventResult>>> TriggerBeforeSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return await ExecuteHandlersAsync(context, 
                (handler, ctx, ct) => handler.BeforeSubmitAsync(ctx, ct), 
                "BeforeSubmit", cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<List<FormEventResult>>> TriggerAfterSubmitAsync(FormEventContext context, CancellationToken cancellationToken = default)
        {
            return await ExecuteHandlersAsync(context, 
                (handler, ctx, ct) => handler.AfterSubmitAsync(ctx, ct), 
                "AfterSubmit", cancellationToken);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IFormEventHandler> GetHandlers(Guid formId)
        {
            if (_formHandlers.TryGetValue(formId, out var handlers))
            {
                lock (_lock)
                {
                    return handlers.ToList().AsReadOnly();
                }
            }
            return new List<IFormEventHandler>().AsReadOnly();
        }

        /// <inheritdoc/>
        public IReadOnlyList<IFormEventHandler> GetGlobalHandlers()
        {
            lock (_lock)
            {
                return _globalHandlers.ToList().AsReadOnly();
            }
        }

        private async Task<Result<List<FormEventResult>>> ExecuteHandlersAsync(
            FormEventContext context,
            Func<IFormEventHandler, FormEventContext, CancellationToken, Task<FormEventResult>> handlerAction,
            string eventName,
            CancellationToken cancellationToken)
        {
            try
            {
                var results = new List<FormEventResult>();
                var allHandlers = new List<IFormEventHandler>();

                // Add global handlers
                lock (_lock)
                {
                    allHandlers.AddRange(_globalHandlers);
                }

                // Add form-specific handlers
                if (_formHandlers.TryGetValue(context.FormId, out var formHandlers))
                {
                    lock (_lock)
                    {
                        allHandlers.AddRange(formHandlers);
                    }
                }

                _logger.LogDebug("Executing {EventName} event for form {FormId} with {HandlerCount} handlers", 
                    eventName, context.FormId, allHandlers.Count);

                foreach (var handler in allHandlers)
                {
                    try
                    {
                        var result = await handlerAction(handler, context, cancellationToken);
                        results.Add(result);

                        // For BeforeSubmit events, check if any handler cancelled the operation
                        if (eventName == "BeforeSubmit" && context.Cancel)
                        {
                            _logger.LogInformation("Form submission cancelled by handler {HandlerType} for form {FormId}. Reason: {Reason}", 
                                handler.GetType().Name, context.FormId, context.CancellationReason);
                            break;
                        }

                        // Update form data if requested
                        if (result.UpdateFormData && result.UpdatedFormData.Any())
                        {
                            foreach (var kvp in result.UpdatedFormData)
                            {
                                context.FormData[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing {EventName} event handler {HandlerType} for form {FormId}", 
                            eventName, handler.GetType().Name, context.FormId);
                        
                        results.Add(new FormEventResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                return Result<List<FormEventResult>>.Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing {EventName} event for form {FormId}", eventName, context.FormId);
                return Result<List<FormEventResult>>.Failure($"Error executing {eventName} event: {ex.Message}");
            }
        }
    }
}