using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.MappingModule.Application.Services;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.SharedKernel.Context;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class ServiceTaskStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<ServiceTaskStepHandler> _logger;
    private readonly IApiProxy _apiProxy;
    private readonly IMappingExecutionService _mappingService;

    public ServiceTaskStepHandler(
        ILogger<ServiceTaskStepHandler> logger,
        IApiProxy apiProxy,
        IMappingExecutionService mappingService)
    {
        _logger = logger;
        _apiProxy = apiProxy;
        _mappingService = mappingService;
    }

    public string StepType => "ServiceTaskStep";

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, StepDsl step, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing ServiceTask {StepId} for workflow {RunId}", step.Id, run.Id);

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
        var apiResult = await _apiProxy.InvokeAsync(methodId, apiParams, cancellationToken);

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
            
            var mapResult = await _mappingService.ExecuteMappingAsync(resMapId, ctxDoc.ToString(), cancellationToken);
            
            if (!mapResult.Success)
            {
                return StepExecutionResult.Failure($"Response mapping failed: {mapResult.Error}");
            }

            // Merge output back to workflow context
            if (!string.IsNullOrEmpty(mapResult.OutputJson))
            {
                var outputDoc = new ContextDocument(mapResult.OutputJson);
                var runCtx = new ContextDocument(run.ContextJson);
                runCtx.Merge(outputDoc);
                run.ContextJson = runCtx.ToString();
            }
        }
        else
        {
            // Default behavior: store in context.apis.{stepId}
            var runCtx = new ContextDocument(run.ContextJson);
            try 
            {
                if (!string.IsNullOrEmpty(apiResult.ResponseBody))
                {
                    var json = JsonSerializer.Deserialize<object>(apiResult.ResponseBody);
                    runCtx.Set($"apis.{step.Id}.response", json);
                }
                else
                {
                    runCtx.Set($"apis.{step.Id}.response", apiResult.ResponseBody);
                }
            } 
            catch 
            {
                runCtx.Set($"apis.{step.Id}.response", apiResult.ResponseBody);
            }
            
            run.ContextJson = runCtx.ToString();
        }
        
        string? nextStepId = step.Transitions?.FirstOrDefault()?.To;
        return StepExecutionResult.Success(nextStepId);
    }
}
