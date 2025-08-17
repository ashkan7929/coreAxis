using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Commands
{
    /// <summary>
    /// Handler for triggering form events.
    /// </summary>
    public class TriggerFormEventCommandHandler : IRequestHandler<TriggerFormEventCommand, Result<List<FormEventResult>>>
    {
        private readonly IFormEventManager _formEventManager;
        private readonly ILogger<TriggerFormEventCommandHandler> _logger;

        public TriggerFormEventCommandHandler(
            IFormEventManager formEventManager,
            ILogger<TriggerFormEventCommandHandler> logger)
        {
            _formEventManager = formEventManager ?? throw new ArgumentNullException(nameof(formEventManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<List<FormEventResult>>> Handle(TriggerFormEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Triggering {EventType} event for form {FormId} by user {UserId}", 
                    request.EventType, request.FormId, request.UserId);

                var context = new FormEventContext
                {
                    FormId = request.FormId,
                    UserId = request.UserId,
                    TenantId = request.TenantId,
                    FormData = request.FormData,
                    PreviousFormData = request.PreviousFormData,
                    ChangedField = request.ChangedField,
                    Metadata = request.Metadata,
                    Timestamp = DateTime.UtcNow
                };

                Result<List<FormEventResult>> result = request.EventType switch
                {
                    FormEventType.OnInit => await _formEventManager.TriggerOnInitAsync(context, cancellationToken),
                    FormEventType.OnChange => await _formEventManager.TriggerOnChangeAsync(context, cancellationToken),
                    FormEventType.BeforeSubmit => await _formEventManager.TriggerBeforeSubmitAsync(context, cancellationToken),
                    FormEventType.AfterSubmit => await _formEventManager.TriggerAfterSubmitAsync(context, cancellationToken),
                    _ => Result<List<FormEventResult>>.Failure($"Unknown event type: {request.EventType}")
                };

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully triggered {EventType} event for form {FormId}. {HandlerCount} handlers executed", 
                        request.EventType, request.FormId, result.Value.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to trigger {EventType} event for form {FormId}. Error: {Error}", 
                        request.EventType, request.FormId, result.Error);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling TriggerFormEventCommand for form {FormId} and event {EventType}", 
                    request.FormId, request.EventType);
                return Result<List<FormEventResult>>.Failure($"Error triggering form event: {ex.Message}");
            }
        }
    }
}