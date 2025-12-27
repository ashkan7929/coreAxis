using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.Ports;
using MediatR;
using System.Text.Json;
using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;

namespace CoreAxis.Modules.ApiManager.Application.Services;

public class ApiManagerInvoker : IApiManagerInvoker
{
    private readonly IApiProxy _apiProxy;
    private readonly IMappingClient _mappingClient;

    public ApiManagerInvoker(IApiProxy apiProxy, IMappingClient mappingClient)
    {
        _apiProxy = apiProxy;
        _mappingClient = mappingClient;
    }

    public async Task<ApiInvokeResult> InvokeAsync(
        string apiMethodRef,
        CoreAxisExecutionContext context,
        string inputMappingSetId,
        string outputMappingSetId,
        bool saveStepIO,
        string stepId,
        CancellationToken ct)
    {
        // 1. Resolve Method ID
        if (!Guid.TryParse(apiMethodRef, out var methodId))
        {
            throw new ArgumentException($"Invalid apiMethodRef: {apiMethodRef}. Expected GUID.");
        }

        // 2. Input Mapping
        var contextJson = JsonSerializer.Serialize(context);
        
        MappingExecutionResult? inputMapResult = null;
        if (!string.IsNullOrEmpty(inputMappingSetId) && Guid.TryParse(inputMappingSetId, out var inMapId))
        {
             try 
             {
                 inputMapResult = await _mappingClient.ExecuteMappingAsync(inMapId, contextJson, ct);
             }
             catch (Exception ex)
             {
                 throw new Exception($"Input mapping failed for step {stepId}: {ex.Message}", ex);
             }
        }
        
        // 3. Invoke API with Explicit Request Parts
        ApiProxyResult result;
        if (inputMapResult != null)
        {
             result = await _apiProxy.InvokeWithExplicitRequestAsync(
                 methodId,
                 inputMapResult.Headers,
                 inputMapResult.Query,
                 inputMapResult.BodyJson,
                 null, // RunId not available in context? Add to Meta?
                 stepId,
                 ct);
        }
        else
        {
             // Fallback: Empty params if no mapping
             result = await _apiProxy.InvokeAsync(methodId, new Dictionary<string, object>(), null, stepId, ct);
        }

        // 4. Update Context IO
        if (saveStepIO)
        {
            if (context.Steps == null) context.Steps = new();
            context.Steps[stepId] = new StepContext 
            { 
                Status = result.IsSuccess ? "Success" : "Failed",
                Response = result // Saving full result object
            };
        }

        // 5. Output Mapping
        if (!string.IsNullOrEmpty(outputMappingSetId) && Guid.TryParse(outputMappingSetId, out var outMapId))
        {
             // Create input for output mapping: { response: {...}, context: {...} }
             var mappingInput = new { response = result, context = context };
             var mappingInputJson = JsonSerializer.Serialize(mappingInput);

             MappingExecutionResult outputMapResult;
             try
             {
                 outputMapResult = await _mappingClient.ExecuteMappingAsync(outMapId, mappingInputJson, ct);
             }
             catch (Exception ex)
             {
                 throw new Exception($"Output mapping failed for step {stepId}: {ex.Message}", ex);
             }
             
             // Apply VarsPatch
             if (outputMapResult.VarsPatch != null)
             {
                foreach (var kvp in outputMapResult.VarsPatch)
                {
                    context.Vars[kvp.Key] = kvp.Value;
                }
             }
        }

        return new ApiInvokeResult
        {
            UpdatedContext = context,
            HttpStatusCode = result.StatusCode ?? (result.IsSuccess ? 200 : 500)
        };
    }
}
