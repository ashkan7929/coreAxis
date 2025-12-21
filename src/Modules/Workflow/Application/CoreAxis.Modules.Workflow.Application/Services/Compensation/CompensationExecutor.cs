using CoreAxis.EventBus;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services.Compensation;

public class CompensationExecutor : ICompensationExecutor
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<CompensationExecutor> _logger;
    private readonly IApiProxy _apiProxy;
    private readonly IEventBus _eventBus;

    public CompensationExecutor(
        WorkflowDbContext context, 
        ILogger<CompensationExecutor> logger,
        IApiProxy apiProxy,
        IEventBus eventBus)
    {
        _context = context;
        _logger = logger;
        _apiProxy = apiProxy;
        _eventBus = eventBus;
    }

    public async Task CompensateAsync(WorkflowRun run, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting compensation for workflow run {RunId}", run.Id);

        // 1. Get Definition DSL
        var version = await _context.WorkflowDefinitionVersions
            .Include(v => v.WorkflowDefinition)
            .FirstOrDefaultAsync(v => v.WorkflowDefinition!.Code == run.WorkflowDefinitionCode && v.VersionNumber == run.VersionNumber, cancellationToken);

        if (version == null)
        {
            _logger.LogError("Definition not found for compensation of run {RunId}", run.Id);
            return;
        }

        WorkflowDsl? dsl;
        try
        {
            dsl = JsonSerializer.Deserialize<WorkflowDsl>(version.DslJson);
        }
        catch
        {
            _logger.LogError("Invalid DSL JSON for run {RunId}", run.Id);
            return;
        }

        if (dsl == null) return;

        // 2. Get Executed Steps in reverse order (LIFO)
        var stepsToCompensate = run.Steps
            .Where(s => s.Status == "Completed")
            .OrderByDescending(s => s.EndedAt)
            .ToList();

        foreach (var runStep in stepsToCompensate)
        {
            var stepDsl = dsl.Steps.FirstOrDefault(s => s.Id == runStep.StepId);
            if (stepDsl?.Compensation == null || !stepDsl.Compensation.Any())
            {
                continue;
            }

            _logger.LogInformation("Compensating step {StepId} ({Type})", runStep.StepId, runStep.StepType);

            foreach (var action in stepDsl.Compensation)
            {
                try
                {
                    await ExecuteActionAsync(run, runStep, action, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute compensation action {Type} for step {StepId}", action.Type, runStep.StepId);
                    // Continue with other actions/steps? Usually yes, best effort.
                }
            }
        }

        _logger.LogInformation("Compensation completed for workflow run {RunId}", run.Id);
    }

    private async Task ExecuteActionAsync(WorkflowRun run, WorkflowRunStep step, CompensationActionDsl action, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing compensation action: {Type}", action.Type);

        if (action.Config == null)
        {
            _logger.LogWarning("Compensation action {Type} missing config", action.Type);
            return;
        }

        switch (action.Type.ToLower())
        {
            case "apicall":
                if (action.Config.TryGetValue("methodId", out var methodIdObj) && Guid.TryParse(methodIdObj?.ToString(), out var methodId))
                {
                    var apiParams = new Dictionary<string, object>();
                    // Potentially map inputs from step context or run context?
                    // For now, assume static or simple inputs
                    
                    var result = await _apiProxy.InvokeAsync(methodId, apiParams, run.Id, step.StepId, cancellationToken);
                    if (!result.IsSuccess)
                    {
                        _logger.LogError("Compensation API call failed: {Error}", result.ErrorMessage);
                    }
                    else
                    {
                        _logger.LogInformation("Compensation API call succeeded");
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid config for apicall compensation: missing methodId");
                }
                break;

            case "walletreverse":
                // TODO: Call WalletModule
                _logger.LogInformation("Simulating Wallet Reversal: {Config}", JsonSerializer.Serialize(action.Config));
                break;

            case "paymentrefund":
                // TODO: Call Payment Gateway/Module
                _logger.LogInformation("Simulating Payment Refund: {Config}", JsonSerializer.Serialize(action.Config));
                break;

            case "customevent":
                if (action.Config.TryGetValue("eventName", out var eventNameObj))
                {
                    var eventName = eventNameObj?.ToString();
                    if (!string.IsNullOrEmpty(eventName))
                    {
                        _logger.LogInformation("Publishing custom compensation event: {EventName}", eventName);
                        
                        var payload = new 
                        { 
                            WorkflowRunId = run.Id, 
                            StepId = step.StepId, 
                            Action = "Compensation",
                            Config = action.Config 
                        };
                        
                        await _eventBus.PublishDynamicAsync(payload, eventName);
                    }
                }
                break;

            default:
                _logger.LogWarning("Unknown compensation action type: {Type}", action.Type);
                break;
        }
    }
}
