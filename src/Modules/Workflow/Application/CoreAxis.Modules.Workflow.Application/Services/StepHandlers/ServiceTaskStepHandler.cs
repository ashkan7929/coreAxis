using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.MappingModule.Application.Services;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.SharedKernel.Context;
using CoreAxis.Modules.Workflow.Application.Idempotency;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class ServiceTaskStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<ServiceTaskStepHandler> _logger;
    private readonly IApiProxy _apiProxy;
    private readonly IMappingExecutionService _mappingService;
    private readonly IIdempotencyService _idempotencyService;

    public ServiceTaskStepHandler(
        ILogger<ServiceTaskStepHandler> logger,
        IApiProxy apiProxy,
        IMappingExecutionService mappingService,
        IIdempotencyService idempotencyService)
    {
        _logger = logger;
        _apiProxy = apiProxy;
        _mappingService = mappingService;
        _idempotencyService = idempotencyService;
    }

    public string StepType => "ServiceTaskStep";

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing ServiceTask {StepId} for workflow {RunId}", step.Id, run.Id);

        if (!string.IsNullOrEmpty(runStep.ExecutionKey))
        {
            var (found, statusCode, responseJson) = await _idempotencyService.TryGetAsync("ServiceTaskStep", runStep.ExecutionKey, "", cancellationToken);
            if (found && statusCode >= 200 && statusCode < 300)
            {
                _logger.LogInformation("Idempotency hit for ServiceTask {StepId} Key {Key}", step.Id, runStep.ExecutionKey);
                var output = string.IsNullOrEmpty(responseJson) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                return StepExecutionResult.Success(step.Transitions?.FirstOrDefault()?.To, output);
            }
        }

        if (step.Config == null)
        {
            return StepExecutionResult.Failure("Missing configuration for ServiceTask");
        }

        if (!step.Config.TryGetValue("serviceMethodId", out var methodIdObj) || 
            !Guid.TryParse(methodIdObj?.ToString(), out var methodId))
        {
             return StepExecutionResult.Failure("Missing or invalid serviceMethodId");
        }

        var apiParams = new Dictionary<string, object>();

        // Request Mapping
        if (step.Config.TryGetValue("requestMappingId", out var reqMapIdObj) && 
            Guid.TryParse(reqMapIdObj?.ToString(), out var reqMapId))
        {
            var mapResult = await _mappingService.ExecuteMappingAsync(reqMapId, run.ContextJson, cancellationToken);
            if (!mapResult.Success)
            {
                return StepExecutionResult.Failure($"Request mapping failed: {mapResult.Error}");
            }
            
            if (!string.IsNullOrEmpty(mapResult.OutputJson))
            {
                apiParams = JsonSerializer.Deserialize<Dictionary<string, object>>(mapResult.OutputJson) ?? new();
            }
        }

        // Invoke API
        var apiResult = await _apiProxy.InvokeAsync(methodId, apiParams, run.Id, step.Id, cancellationToken);

        if (!apiResult.IsSuccess)
        {
            return StepExecutionResult.Failure($"API call failed: {apiResult.ErrorMessage} (Status: {apiResult.StatusCode})");
        }

        // Response Mapping
        if (step.Config.TryGetValue("responseMappingId", out var resMapIdObj) && 
            Guid.TryParse(resMapIdObj?.ToString(), out var resMapId))
        {
            // Create a merged context for mapping: { ...originalContext, "response": apiResponse }
            // Or strictly pass response as separate root?
            // TransformEngine usually takes one JSON.
            // Let's create a temporary JSON with response injected.
            
            // Using ContextDocument to manipulate
            var ctxDoc = new ContextDocument(run.ContextJson);
            
            // Try to parse response body as JSON, otherwise string
            object responseData = apiResult.ResponseBody ?? string.Empty;
            try 
            {
                if (!string.IsNullOrEmpty(apiResult.ResponseBody))
                    responseData = JsonSerializer.Deserialize<object>(apiResult.ResponseBody) ?? apiResult.ResponseBody;
            } 
            catch {}

            ctxDoc.Set("response", responseData);
            
            var mapResult = await _mappingService.ExecuteMappingAsync(resMapId, ctxDoc.ToJson(), cancellationToken);
            
            if (!mapResult.Success)
            {
                return StepExecutionResult.Failure($"Response mapping failed: {mapResult.Error}");
            }

            // Merge output back to workflow context
            if (!string.IsNullOrEmpty(mapResult.OutputJson))
            {
                var output = JsonSerializer.Deserialize<Dictionary<string, object>>(mapResult.OutputJson);
                
                // Store result for idempotency
                if (!string.IsNullOrEmpty(runStep.ExecutionKey))
                {
                    await _idempotencyService.StoreAsync("ServiceTaskStep", runStep.ExecutionKey, "", 200, JsonSerializer.Serialize(output), cancellationToken);
                }

                return StepExecutionResult.Success(step.Transitions?.FirstOrDefault()?.To, output);
            }
        }
        else
        {
            // No mapping, just put response in context if needed, or nothing
            // Maybe we should store apiResult.ResponseBody as output if no mapping?
            // For now, assume mapping is required or nothing is returned to context.
            
            // If no response mapping, we still might want to be idempotent?
            // Yes, preventing re-execution.
            if (!string.IsNullOrEmpty(runStep.ExecutionKey))
            {
                await _idempotencyService.StoreAsync("ServiceTaskStep", runStep.ExecutionKey, "", 200, "{}", cancellationToken);
            }
        }

        return StepExecutionResult.Success(step.Transitions?.FirstOrDefault()?.To);
    }
}
