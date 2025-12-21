using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.MappingModule.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Forms;

public class PrefillFormCommandHandler : IRequestHandler<PrefillFormCommand, Result<Dictionary<string, object>>>
{
    private readonly IFormRepository _formRepository;
    private readonly IApiProxy _apiProxy;
    private readonly IMappingExecutionService _mappingService;
    private readonly ILogger<PrefillFormCommandHandler> _logger;

    public PrefillFormCommandHandler(
        IFormRepository formRepository,
        IApiProxy apiProxy,
        IMappingExecutionService mappingService,
        ILogger<PrefillFormCommandHandler> logger)
    {
        _formRepository = formRepository;
        _apiProxy = apiProxy;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<Dictionary<string, object>>> Handle(PrefillFormCommand request, CancellationToken cancellationToken)
    {
        var form = await _formRepository.GetByIdAsync(request.FormId, cancellationToken);
        if (form == null) return Result<Dictionary<string, object>>.Failure("Form not found");

        var resultData = new Dictionary<string, object>();

        foreach (var step in request.Steps)
        {
            try
            {
                // 1. Invoke API
                // Merge request context with step input context
                var stepParameters = new Dictionary<string, object>(request.Context);
                foreach (var kvp in step.InputContext)
                {
                    stepParameters[kvp.Key] = kvp.Value;
                }

                var apiResult = await _apiProxy.InvokeAsync(step.ApiMethodId, stepParameters, null, null, cancellationToken);

                if (!apiResult.IsSuccess)
                {
                    _logger.LogWarning("Prefill API call failed for MethodId {MethodId}: {Error}", step.ApiMethodId, apiResult.ErrorMessage);
                    continue; // Skip this step or fail? Usually best effort for prefill.
                }

                // 2. Map Response
                if (!string.IsNullOrEmpty(apiResult.ResponseBody))
                {
                    // Mapping service expects JSON context. We'll wrap the API response.
                    var mappingContext = new
                    {
                        apiResponse = JsonSerializer.Deserialize<object>(apiResult.ResponseBody),
                        context = stepParameters
                    };
                    
                    var mappingContextJson = JsonSerializer.Serialize(mappingContext);
                    var mappingResult = await _mappingService.ExecuteMappingAsync(step.MappingId, mappingContextJson, cancellationToken);

                    if (mappingResult.Success && !string.IsNullOrEmpty(mappingResult.OutputJson))
                    {
                        var mappedValues = JsonSerializer.Deserialize<Dictionary<string, object>>(mappingResult.OutputJson);
                        if (mappedValues != null)
                        {
                            foreach (var kvp in mappedValues)
                            {
                                resultData[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Prefill mapping failed for MappingId {MappingId}: {Error}", step.MappingId, mappingResult.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing prefill step for API {ApiMethodId}", step.ApiMethodId);
            }
        }

        return Result<Dictionary<string, object>>.Success(resultData);
    }
}
