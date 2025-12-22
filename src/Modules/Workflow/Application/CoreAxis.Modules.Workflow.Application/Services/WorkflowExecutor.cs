using CoreAxis.Modules.Workflow.Application.Services.StepHandlers;
using CoreAxis.Modules.Workflow.Application.Services.Compensation;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Infrastructure.Data;

namespace CoreAxis.Modules.Workflow.Application.Services;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly WorkflowDbContext _context;
    private readonly IEnumerable<IWorkflowStepHandler> _handlers;
    private readonly ICompensationExecutor _compensationExecutor;
    private readonly ILogger<WorkflowExecutor> _logger;

    public WorkflowExecutor(
        WorkflowDbContext context,
        IEnumerable<IWorkflowStepHandler> handlers,
        ICompensationExecutor compensationExecutor,
        ILogger<WorkflowExecutor> logger)
    {
        _context = context;
        _handlers = handlers;
        _compensationExecutor = compensationExecutor;
        _logger = logger;
    }

    public async Task ExecuteStepAsync(Guid workflowRunId, string stepId, CancellationToken cancellationToken = default)
    {
        var run = await _context.WorkflowRuns
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == workflowRunId, cancellationToken);

        if (run == null)
        {
            _logger.LogError("Workflow run {RunId} not found", workflowRunId);
            return;
        }

        var version = await _context.WorkflowDefinitionVersions
            .Include(v => v.WorkflowDefinition)
            .FirstOrDefaultAsync(v => v.WorkflowDefinition!.Code == run.WorkflowDefinitionCode && v.VersionNumber == run.VersionNumber, cancellationToken);

        if (version == null)
        {
            _logger.LogError("Workflow definition version not found for run {RunId}", workflowRunId);
            return;
        }

        WorkflowDsl? dsl;
        try
        {
            dsl = JsonSerializer.Deserialize<WorkflowDsl>(version.DslJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize workflow DSL for run {RunId}", workflowRunId);
            return;
        }

        if (dsl == null) return;

        // "start" is a virtual step to find the first actual step
        if (stepId == "start")
        {
            var firstStep = dsl.Steps.FirstOrDefault(s => s.Id == dsl.StartAt);
            if (firstStep != null)
            {
                await ExecuteStepInternalAsync(run, firstStep, cancellationToken);
            }
            else
            {
                _logger.LogError("Start step {StartAt} not found in DSL", dsl.StartAt);
            }
        }
        else
        {
            var step = dsl.Steps.FirstOrDefault(s => s.Id == stepId);
            if (step != null)
            {
                await ExecuteStepInternalAsync(run, step, cancellationToken);
            }
            else
            {
                _logger.LogError("Step {StepId} not found in DSL", stepId);
            }
        }
    }

    private async Task ExecuteStepInternalAsync(WorkflowRun run, StepDsl stepDsl, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing step {StepId} ({Type}) for run {RunId}", stepDsl.Id, stepDsl.Type, run.Id);

        var handler = _handlers.FirstOrDefault(h => h.StepType == stepDsl.Type);
        if (handler == null)
        {
            _logger.LogError("No handler found for step type {Type}", stepDsl.Type);
            run.Fail($"No handler found for step type {stepDsl.Type}");
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingSteps = await _context.WorkflowRunSteps
            .CountAsync(s => s.WorkflowRunId == run.Id && s.StepId == stepDsl.Id, cancellationToken);
        
        var attempt = existingSteps + 1;
        var executionKey = $"{run.Id}:{stepDsl.Id}:{attempt}";

        var runStep = new WorkflowRunStep
        {
            WorkflowRunId = run.Id,
            StepId = stepDsl.Id,
            StepType = stepDsl.Type,
            Status = "Running",
            Attempts = attempt,
            ExecutionKey = executionKey,
            StartedAt = DateTime.UtcNow,
            CreatedBy = "System",
            LastModifiedBy = "System"
        };
        _context.WorkflowRunSteps.Add(runStep);
        await _context.SaveChangesAsync(cancellationToken);

        StepExecutionResult result;
        try
        {
            result = await handler.ExecuteAsync(run, runStep, stepDsl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step {StepId}", stepDsl.Id);
            result = StepExecutionResult.Failure(ex.Message);
        }

        if (result.IsSuccess)
        {
            if (result.IsPaused)
            {
                runStep.Status = "Paused";
                run.Pause(stepDsl.Id);
                _logger.LogInformation("Workflow run {RunId} paused at step {StepId}", run.Id, stepDsl.Id);
            }
            else
            {
                runStep.Status = "Completed";
                runStep.EndedAt = DateTime.UtcNow;
                
                // Merge output context if any
                if (result.OutputContext != null)
                {
                    // Simple merge logic: update/add keys
                    var currentContext = JsonSerializer.Deserialize<Dictionary<string, object>>(run.ContextJson) ?? new Dictionary<string, object>();
                    foreach (var kvp in result.OutputContext)
                    {
                        currentContext[kvp.Key] = kvp.Value;
                    }
                    run.ContextJson = JsonSerializer.Serialize(currentContext);
                }

                if (!string.IsNullOrEmpty(result.NextStepId))
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    // Recursive call for MVP - in production use a queue
                    await ExecuteStepAsync(run.Id, result.NextStepId, cancellationToken);
                    return; // Return to avoid double save at end of method
                }
                else
                {
                    run.Complete();
                    _logger.LogInformation("Workflow run {RunId} completed", run.Id);
                }
            }
        }
        else
        {
            runStep.Status = "Failed";
            runStep.Error = result.Error;
            run.Fail(result.Error ?? "Unknown error");
            _logger.LogError("Workflow run {RunId} failed at step {StepId}: {Error}", run.Id, stepDsl.Id, result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Check if failed and trigger compensation
        if (run.Status == "Failed")
        {
             await _compensationExecutor.CompensateAsync(run, cancellationToken);
        }
    }

    public async Task ResumeAsync(Guid workflowRunId, Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        await ResumeInternalAsync(workflowRunId, input, "Resume", cancellationToken);
    }

    public async Task SignalAsync(Guid workflowRunId, string signalName, Dictionary<string, object> payload, CancellationToken cancellationToken = default)
    {
        // 1. Log signal
        var signal = new WorkflowSignal
        {
            WorkflowRunId = workflowRunId,
            Name = signalName,
            PayloadJson = JsonSerializer.Serialize(payload),
            HandledAt = DateTime.UtcNow
        };
        _context.WorkflowSignals.Add(signal);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Try to resume if paused
        // For MVP, we treat any signal as a potential resume trigger for the current paused step.
        // In a real system, we'd check if the current step is waiting for THIS specific signal.
        // For WaitForEventStep, we assume it's waiting for any signal or we check the signal name if configured.
        
        await ResumeInternalAsync(workflowRunId, payload, signalName, cancellationToken);
    }

    public async Task SignalByCorrelationAsync(string correlationId, string signalName, Dictionary<string, object> payload, CancellationToken cancellationToken = default)
    {
        // Find the latest running/paused workflow with this correlation ID
        var run = await _context.WorkflowRuns
            .Where(r => r.CorrelationId == correlationId && (r.Status == "Running" || r.Status == "Paused"))
            .OrderByDescending(r => r.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (run == null)
        {
            _logger.LogWarning("No running or paused workflow found with correlation ID {CorrelationId} to signal {SignalName}", correlationId, signalName);
            return;
        }

        _logger.LogInformation("Signaling workflow {RunId} (Correlation: {CorrelationId}) with {SignalName}", run.Id, correlationId, signalName);
        await SignalAsync(run.Id, signalName, payload, cancellationToken);
    }

    public async Task CancelAsync(Guid workflowRunId, string reason, CancellationToken cancellationToken = default)
    {
        var run = await _context.WorkflowRuns
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == workflowRunId, cancellationToken);

        if (run == null)
        {
            _logger.LogError("Workflow run {RunId} not found", workflowRunId);
            return;
        }

        if (run.Status == "Completed" || run.Status == "Failed" || run.Status == "Cancelled")
        {
            _logger.LogWarning("Cannot cancel workflow {RunId} because it is already {Status}", workflowRunId, run.Status);
            return;
        }

        // Cancel running/paused steps
        var activeSteps = run.Steps.Where(s => s.Status == "Running" || s.Status == "Paused").ToList();
        foreach (var step in activeSteps)
        {
            step.Status = "Cancelled";
            step.EndedAt = DateTime.UtcNow;
            step.Error = "Workflow cancelled";
        }

        run.Cancel(reason);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow run {RunId} cancelled. Reason: {Reason}", workflowRunId, reason);

        // Trigger compensation
        await _compensationExecutor.CompensateAsync(run, cancellationToken);
    }

    private async Task ResumeInternalAsync(Guid workflowRunId, Dictionary<string, object> input, string signalName, CancellationToken cancellationToken)
    {
        var run = await _context.WorkflowRuns
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == workflowRunId, cancellationToken);

        if (run == null)
        {
            _logger.LogError("Workflow run {RunId} not found", workflowRunId);
            return;
        }

        if (run.Status != "Paused")
        {
            _logger.LogWarning("Cannot resume workflow {RunId} because it is not paused (Status: {Status})", workflowRunId, run.Status);
            return;
        }

        var pausedStep = run.Steps.OrderByDescending(s => s.StartedAt).FirstOrDefault(s => s.Status == "Paused" || s.Status == "Running");
        if (pausedStep == null)
        {
            _logger.LogWarning("No paused step found for run {RunId}", workflowRunId);
            return;
        }

        // Update context with input
        var currentContext = JsonSerializer.Deserialize<Dictionary<string, object>>(run.ContextJson) ?? new Dictionary<string, object>();
        foreach (var kvp in input)
        {
            currentContext[kvp.Key] = kvp.Value;
        }
        run.ContextJson = JsonSerializer.Serialize(currentContext);

        run.Resume(signalName);

        pausedStep.Status = "Completed";
        pausedStep.EndedAt = DateTime.UtcNow;

        var version = await _context.WorkflowDefinitionVersions
            .Include(v => v.WorkflowDefinition)
            .FirstOrDefaultAsync(v => v.WorkflowDefinition!.Code == run.WorkflowDefinitionCode && v.VersionNumber == run.VersionNumber, cancellationToken);

        if (version == null) return;

        WorkflowDsl? dsl;
        try { dsl = JsonSerializer.Deserialize<WorkflowDsl>(version.DslJson); } catch { return; }
        if (dsl == null) return;

        var stepDsl = dsl.Steps.FirstOrDefault(s => s.Id == pausedStep.StepId);
        if (stepDsl == null) return;

        // Determine next step
        string? nextStepId = null;
        if (stepDsl.Transitions != null && stepDsl.Transitions.Any())
        {
             // Simple logic: take the first unconditional transition or just the first one
             nextStepId = stepDsl.Transitions.First().To;
        }

        if (nextStepId != null)
        {
            // Already set to Running in Resume()
            await _context.SaveChangesAsync(cancellationToken);
            await ExecuteStepAsync(workflowRunId, nextStepId, cancellationToken);
        }
        else
        {
            run.Complete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
