using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.Modules.Workflow.Application.Services.StepHandlers;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services.StepHandlers;

public class TimerStepHandler : IWorkflowStepHandler
{
    private readonly IRepository<WorkflowTimer> _timerRepository;

    public TimerStepHandler(IRepository<WorkflowTimer> timerRepository)
    {
        _timerRepository = timerRepository;
    }

    public string StepType => "TimerStep";

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowRun run, WorkflowRunStep runStep, StepDsl step, CancellationToken cancellationToken)
    {
        if (step.Config == null || !step.Config.ContainsKey("duration"))
        {
            return StepExecutionResult.Failure("Timer step missing 'duration' configuration.");
        }

        var durationStr = step.Config["duration"].ToString();
        if (!TimeSpan.TryParse(durationStr, out var duration))
        {
             return StepExecutionResult.Failure($"Invalid duration format: {durationStr}");
        }

        var dueAt = DateTime.UtcNow.Add(duration);

        var timer = new WorkflowTimer
        {
            WorkflowRunId = run.Id,
            StepId = step.Id,
            DueAt = dueAt,
            SignalName = $"Timer_{step.Id}",
            Status = "Pending",
            PayloadJson = JsonSerializer.Serialize(new { NextStepId = step.Transitions?.FirstOrDefault()?.To }),
            CreatedBy = "System",
            CreatedOn = DateTime.UtcNow,
            LastModifiedBy = "System",
            IsActive = true
        };

        await _timerRepository.AddAsync(timer);

        return StepExecutionResult.Pause();
    }
}
