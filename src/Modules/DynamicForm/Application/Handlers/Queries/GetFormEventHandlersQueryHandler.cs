using CoreAxis.Modules.DynamicForm.Application.Queries.Forms;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Queries
{
    /// <summary>
    /// Handler for getting form event handlers information.
    /// </summary>
    public class GetFormEventHandlersQueryHandler : IRequestHandler<GetFormEventHandlersQuery, Result<FormEventHandlersDto>>
    {
        private readonly IFormEventManager _formEventManager;
        private readonly ILogger<GetFormEventHandlersQueryHandler> _logger;

        public GetFormEventHandlersQueryHandler(
            IFormEventManager formEventManager,
            ILogger<GetFormEventHandlersQueryHandler> logger)
        {
            _formEventManager = formEventManager ?? throw new ArgumentNullException(nameof(formEventManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<FormEventHandlersDto>> Handle(GetFormEventHandlersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting event handlers for form {FormId}", request.FormId);

                var formHandlers = _formEventManager.GetHandlers(request.FormId);
                var globalHandlers = _formEventManager.GetGlobalHandlers();

                var dto = new FormEventHandlersDto
                {
                    FormId = request.FormId,
                    FormHandlers = formHandlers.Select(h => CreateHandlerInfo(h, false)).ToList(),
                    GlobalHandlers = globalHandlers.Select(h => CreateHandlerInfo(h, true)).ToList()
                };

                dto.TotalHandlers = dto.FormHandlers.Count + dto.GlobalHandlers.Count;

                _logger.LogInformation("Found {TotalHandlers} handlers for form {FormId} ({FormHandlers} form-specific, {GlobalHandlers} global)", 
                    dto.TotalHandlers, request.FormId, dto.FormHandlers.Count, dto.GlobalHandlers.Count);

                return await Task.FromResult(Result<FormEventHandlersDto>.Success(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event handlers for form {FormId}", request.FormId);
                return Result<FormEventHandlersDto>.Failure($"Error getting form event handlers: {ex.Message}");
            }
        }

        private static FormEventHandlerInfo CreateHandlerInfo(IFormEventHandler handler, bool isGlobal)
        {
            var handlerType = handler.GetType();
            var description = handlerType.GetCustomAttribute<DescriptionAttribute>()?.Description;

            return new FormEventHandlerInfo
            {
                TypeName = handlerType.Name,
                AssemblyName = handlerType.Assembly.GetName().Name ?? "Unknown",
                IsGlobal = isGlobal,
                Description = description
            };
        }
    }
}