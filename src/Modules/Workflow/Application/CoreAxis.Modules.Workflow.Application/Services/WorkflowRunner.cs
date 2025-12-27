using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Domain.Repositories;
using CoreAxis.SharedKernel.Ports;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;
using CoreAxis.SharedKernel.Context;

namespace CoreAxis.Modules.Workflow.Application.Services;

public class WorkflowRunner : IWorkflowRunner
{
    private readonly IWorkflowDefinitionRepository _repository;
    private readonly IApiManagerInvoker _apiManagerInvoker;
    private readonly IMappingClient _mappingClient;

    public WorkflowRunner(
        IWorkflowDefinitionRepository repository,
        IApiManagerInvoker apiManagerInvoker,
        IMappingClient mappingClient)
    {
        _repository = repository;
        _apiManagerInvoker = apiManagerInvoker;
        _mappingClient = mappingClient;
    }

    public async Task<WorkflowRunResult> RunAsync(
        string workflowCode,
        int workflowVersion,
        CoreAxisExecutionContext context,
        CancellationToken ct)
    {
        var definitionVersion = await _repository.GetVersionAsync(workflowCode, workflowVersion, ct);
        if (definitionVersion == null)
        {
            return new WorkflowRunResult
            {
                Success = false,
                ErrorCode = "WORKFLOW_NOT_FOUND",
                ErrorMessage = $"Workflow {workflowCode} v{workflowVersion} not found",
                Context = context
            };
        }

        WorkflowDsl? dsl;
        try
        {
            dsl = JsonSerializer.Deserialize<WorkflowDsl>(definitionVersion.DslJson);
        }
        catch (Exception ex)
        {
            return new WorkflowRunResult
            {
                Success = false,
                ErrorCode = "DSL_PARSE_ERROR",
                ErrorMessage = ex.Message,
                Context = context
            };
        }

        if (dsl == null || dsl.Steps == null)
        {
            return new WorkflowRunResult
            {
                Success = false,
                ErrorCode = "INVALID_DSL",
                ErrorMessage = "DSL is empty or invalid",
                Context = context
            };
        }

        string? currentStepId = dsl.StartAt ?? dsl.Steps.FirstOrDefault()?.Id;
        
        while (!string.IsNullOrEmpty(currentStepId))
        {
            var step = dsl.Steps.FirstOrDefault(s => s.Id == currentStepId);
            if (step == null) break;

            // Execute Step
            if (step.Type == "apiCall")
            {
                var config = DeserializeConfig<ApiCallStepConfig>(step.Config);
                if (config == null) throw new Exception($"Invalid config for step {step.Id}");

                var result = await _apiManagerInvoker.InvokeAsync(
                    config.ApiMethodRef,
                    context,
                    config.InputMappingSetId,
                    config.OutputMappingSetId,
                    config.SaveStepIO,
                    step.Id,
                    ct);

                context = result.UpdatedContext;

                if (result.HttpStatusCode >= 400)
                {
                    return new WorkflowRunResult
                    {
                        Success = false,
                        ErrorCode = "API_ERROR",
                        ErrorMessage = $"Step {step.Id} failed with status {result.HttpStatusCode}",
                        Context = context
                    };
                }
            }
            else if (step.Type == "return")
            {
                 var config = DeserializeConfig<ReturnStepConfig>(step.Config);
                 if (config != null && !string.IsNullOrEmpty(config.OutputMappingSetId))
                 {
                     if (Guid.TryParse(config.OutputMappingSetId, out var mapId))
                     {
                         var contextJson = JsonSerializer.Serialize(context);
                         string mappedJson = "{}";
                         try {
                             var mapResult = await _mappingClient.ExecuteMappingAsync(mapId, contextJson, ct);
                             mappedJson = mapResult.BodyJson;
                         } catch (Exception) {
                             // Ignore error or log it? 
                             // For now we proceed, assuming mapping might be optional or failure handled upstream
                         }
                         
                         var mappedObj = JsonSerializer.Deserialize<Dictionary<string, object>>(mappedJson);
                         if (mappedObj != null)
                         {
                             foreach(var kv in mappedObj) context.Vars[kv.Key] = kv.Value;
                         }
                     }
                 }
                 
                 return new WorkflowRunResult
                 {
                     Success = true,
                     Context = context
                 };
            }
            
            // Determine next step
            if (step.Transitions != null && step.Transitions.Any())
            {
                 currentStepId = step.Transitions.First().To; // Naive
            }
            else
            {
                 var index = dsl.Steps.IndexOf(step);
                 if (index >= 0 && index < dsl.Steps.Count - 1)
                 {
                     currentStepId = dsl.Steps[index + 1].Id;
                 }
                 else
                 {
                     currentStepId = null;
                 }
            }
        }

        return new WorkflowRunResult
        {
            Success = true,
            Context = context
        };
    }

    private T? DeserializeConfig<T>(Dictionary<string, object>? config)
    {
        if (config == null) return default;
        var json = JsonSerializer.Serialize(config);
        return JsonSerializer.Deserialize<T>(json);
    }
}

public class ApiCallStepConfig
{
    [JsonPropertyName("apiMethodRef")]
    public string ApiMethodRef { get; set; } = "";
    
    [JsonPropertyName("inputMappingSetId")]
    public string InputMappingSetId { get; set; } = "";
    
    [JsonPropertyName("outputMappingSetId")]
    public string OutputMappingSetId { get; set; } = "";
    
    [JsonPropertyName("saveStepIO")]
    public bool SaveStepIO { get; set; }
    
    [JsonPropertyName("resultVar")]
    public string ResultVar { get; set; } = "";
}

public class ReturnStepConfig
{
    [JsonPropertyName("outputMappingSetId")]
    public string OutputMappingSetId { get; set; } = "";
}
