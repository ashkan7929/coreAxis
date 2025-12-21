using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.Workflow.Application.Services;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Api.Services;

public class WorkflowTimerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowTimerBackgroundService> _logger;

    public WorkflowTimerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowTimerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow Timer Background Service starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTimersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workflow timers.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Workflow Timer Background Service stopping.");
    }

    private async Task ProcessTimersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var executor = scope.ServiceProvider.GetRequiredService<IWorkflowExecutor>();

        var dueTimers = await context.WorkflowTimers
            .Where(t => t.Status == "Pending" && t.DueAt <= DateTime.UtcNow)
            .OrderBy(t => t.DueAt)
            .Take(50)
            .ToListAsync(stoppingToken);

        if (!dueTimers.Any()) return;

        _logger.LogInformation("Found {Count} due timers.", dueTimers.Count);

        foreach (var timer in dueTimers)
        {
            try
            {
                var payload = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(timer.PayloadJson))
                {
                    payload = JsonSerializer.Deserialize<Dictionary<string, object>>(timer.PayloadJson) ?? new();
                }

                // Signal the workflow to resume
                await executor.SignalAsync(timer.WorkflowRunId, timer.SignalName, payload, stoppingToken);

                // Update timer status
                timer.Status = "Processed";
                timer.LastModifiedOn = DateTime.UtcNow;
                
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing timer {TimerId} for workflow {WorkflowRunId}", timer.Id, timer.WorkflowRunId);
            }
        }
    }
}
