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

                // Handle assignTo (save response to specific variable path)
                if (!string.IsNullOrEmpty(config.AssignTo))
                {
                    // Logic to assign result.UpdatedContext.Form/Vars to config.AssignTo
                    // But result.UpdatedContext is already the *whole* context after mapping.
                    // If outputMappingSetId was used, ApiManagerInvoker already mapped output to Context.
                    // If assignTo is used *instead* of mapping (or as a simple override), we need to extract the response.
                    // However, IApiManagerInvoker signature returns UpdatedContext.
                    // If we want to support direct assignment like vars["x"] = response,
                    // we might need to know *what* was the response before it was merged.
                    // BUT, IApiManagerInvoker logic inside usually handles mapping.
                    // If assignTo is provided, we might assume the USER wants the raw response or the mapped result 
                    // placed in a specific var.
                    
                    // Since IApiManagerInvoker encapsulates the mapping logic, let's assume 
                    // if assignTo is present, we should probably check if we can get the 'response' 
                    // from the step context or similar?
                    // Actually, the requirement says: "assignTo (optional; e.g. put output in vars["fanavaran.login"])"
                    
                    // If ApiManagerInvoker did its job, 'context' is updated.
                    // If we want to move something to 'assignTo', we need to access it.
                    // Let's assume for now that if assignTo is set, we might need to manually set it 
                    // if ApiManagerInvoker didn't already place it there.
                    // But wait, ApiManagerInvoker takes 'stepId'. Maybe it stores response in steps[stepId].response?
                    // Let's check ExecutionContext structure. Yes: Steps[stepId].Response
                    
                    if (context.Steps.TryGetValue(step.Id, out var stepContext) && stepContext.Response != null)
                    {
                         // Simple path setting (supporting vars.x or just x)
                         if (config.AssignTo.StartsWith("vars."))
                         {
                             var key = config.AssignTo.Substring(5);
                             context.Vars[key] = stepContext.Response;
                         }
                         else
                         {
                             context.Vars[config.AssignTo] = stepContext.Response;
                         }
                    }
                }
            }
            else if (step.Type == "return")
            {
                 var config = DeserializeConfig<ReturnStepConfig>(step.Config);
                 object? output = null;
                 string outputJson = "{}";

                 if (config != null)
                 {
                     if (!string.IsNullOrEmpty(config.OutputMappingSetId) && Guid.TryParse(config.OutputMappingSetId, out var mapId) && mapId != Guid.Empty)
                     {
                         // 1. Mapping Strategy
                         var contextJson = JsonSerializer.Serialize(context);
                         try 
                         {
                             var mapResult = await _mappingClient.ExecuteMappingAsync(mapId, contextJson, ct);
                             outputJson = mapResult.BodyJson ?? "{}";
                             output = JsonSerializer.Deserialize<object>(outputJson);
                         } 
                         catch 
                         {
                             // Fallback or error logging
                             outputJson = "{\"error\": \"Mapping failed\"}";
                         }
                     }
                     else if (!string.IsNullOrEmpty(config.Source))
                     {
                         // 2. Direct Source Strategy (e.g. "vars.myResult")
                         if (config.Source.StartsWith("vars.") && context.Vars.TryGetValue(config.Source.Substring(5), out var val))
                         {
                             output = val;
                         }
                         else if (context.Vars.TryGetValue(config.Source, out var val2))
                         {
                             output = val2;
                         }
                         else if (config.Source == "form")
                         {
                             output = context.Form;
                         }
                         
                         if (output != null)
                         {
                             outputJson = JsonSerializer.Serialize(output);
                         }
                     }
                     else
                     {
                         // 3. Default Strategy: Return full context (or should it be empty?)
                         // User said: "return must build an 'outputJson' ... independent of internals"
                         // If no config provided, maybe returning everything is safe for debugging, 
                         // but ideally we should be explicit. 
                         // Let's default to full context for backward compatibility but ensure it's in outputJson
                         output = context;
                         outputJson = JsonSerializer.Serialize(context);
                     }
                 }
                 else
                 {
                     output = context;
                     outputJson = JsonSerializer.Serialize(context);
                 }
                 
                 return new WorkflowRunResult
                 {
                     Success = true,
                     Context = context,
                     Output = output,
                     OutputJson = outputJson
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
    
    [JsonPropertyName("assignTo")]
    public string AssignTo { get; set; } = "";
}

public class ReturnStepConfig
{
    [JsonPropertyName("outputMappingSetId")]
    public string OutputMappingSetId { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";
}
