using CoreAxis.Modules.ApiManager.Infrastructure;
using CoreAxis.Modules.TaskModule.Infrastructure.Data;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.ApiGateway.Controllers;

[ApiController]
[Route("api/admin/instances/{workflowId}/timeline")]
public class AdminTimelineController : ControllerBase
{
    private readonly WorkflowDbContext _workflowContext;
    private readonly TaskDbContext _taskContext;
    private readonly ApiManagerDbContext _apiManagerContext;
    private readonly ILogger<AdminTimelineController> _logger;

    public AdminTimelineController(
        WorkflowDbContext workflowContext,
        TaskDbContext taskContext,
        ApiManagerDbContext apiManagerContext,
        ILogger<AdminTimelineController> logger)
    {
        _workflowContext = workflowContext;
        _taskContext = taskContext;
        _apiManagerContext = apiManagerContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTimeline(Guid workflowId, [FromQuery] string? correlationId)
    {
        // 1. Fetch Workflow Run
        var run = await _workflowContext.WorkflowRuns
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == workflowId);

        if (run == null)
        {
            return NotFound("Workflow run not found.");
        }

        var timeline = new List<TimelineEntryDto>();

        // 2. Add Workflow Steps
        foreach (var step in run.Steps)
        {
            timeline.Add(new TimelineEntryDto
            {
                Timestamp = step.CreatedOn,
                Module = "Workflow",
                Type = $"Step:{step.StepType}",
                Title = $"Step {step.StepId} ({step.Status})",
                Status = step.Status,
                Details = new { step.StepId, step.StepType, step.Attempts, Error = step.Error },
                CorrelationId = run.CorrelationId
            });
        }

        // 3. Add Tasks
        var tasks = await _taskContext.TaskInstances
            .Where(t => t.WorkflowId == workflowId)
            .ToListAsync();

        foreach (var task in tasks)
        {
            timeline.Add(new TimelineEntryDto
            {
                Timestamp = task.CreatedOn,
                Module = "Task",
                Type = "HumanTask",
                Title = $"Task {task.StepKey} assigned to {task.AssigneeType}:{task.AssigneeId}",
                Status = task.Status,
                Details = new { task.StepKey, task.AssigneeId, task.DueAt },
                CorrelationId = run.CorrelationId
            });

            if (task.CompletedAt.HasValue)
            {
                timeline.Add(new TimelineEntryDto
                {
                    Timestamp = task.CompletedAt.Value,
                    Module = "Task",
                    Type = "HumanTaskCompleted",
                    Title = $"Task {task.StepKey} completed",
                    Status = "Completed",
                    Details = new { task.StepKey, Outcome = "Completed" }, 
                    CorrelationId = run.CorrelationId
                });
            }
        }

        // 4. Add API Logs
        var apiLogs = await _apiManagerContext.WebServiceCallLogs
            .Where(l => l.WorkflowRunId == workflowId)
            .ToListAsync();

        foreach (var log in apiLogs)
        {
            timeline.Add(new TimelineEntryDto
            {
                Timestamp = log.CreatedAt,
                Module = "ApiManager",
                Type = "ApiCall",
                Title = $"API Call: {log.MethodId} ({log.StatusCode})",
                Status = log.Succeeded ? "Success" : "Failed",
                Details = new { log.MethodId, log.LatencyMs, log.Error },
                CorrelationId = log.CorrelationId
            });
        }

        // 5. Sort and Return
        return Ok(timeline.OrderBy(x => x.Timestamp));
    }
}

public class TimelineEntryDto
{
    public DateTime Timestamp { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public object? Details { get; set; }
    public string? CorrelationId { get; set; }
}
