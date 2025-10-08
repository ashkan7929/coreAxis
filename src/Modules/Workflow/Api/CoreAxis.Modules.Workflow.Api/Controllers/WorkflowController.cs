using CoreAxis.SharedKernel.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.IO;

namespace CoreAxis.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowClient _workflowClient;
    private readonly ILogger<WorkflowController> _logger;
    private readonly string _storeRoot;

    public WorkflowController(IWorkflowClient workflowClient, ILogger<WorkflowController> logger)
    {
        _workflowClient = workflowClient;
        _logger = logger;
        _storeRoot = Path.Combine(AppContext.BaseDirectory, "App_Data", "workflows");
        Directory.CreateDirectory(_storeRoot);
    }

    [HttpPost("post-finalize/start")]
    public async Task<IActionResult> StartPostFinalize([FromBody] JsonElement context, CancellationToken cancellationToken)
    {
        var result = await _workflowClient.StartAsync("post-finalize-workflow", context, cancellationToken);
        _logger.LogInformation("Started post-finalize workflow {WorkflowId}", result.WorkflowId);
        return Ok(result);
    }

    [HttpPost("{workflowId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid workflowId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { workflowId }));
        var result = await _workflowClient.SignalAsync("Resume", payload, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{workflowId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid workflowId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { workflowId }));
        var result = await _workflowClient.SignalAsync("Cancel", payload, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{workflowId:guid}")]
    public async Task<IActionResult> GetStatus(Guid workflowId, CancellationToken cancellationToken)
    {
        var result = await _workflowClient.GetWorkflowStatusAsync(workflowId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{workflowId:guid}/history")]
    public IActionResult GetHistory(Guid workflowId)
    {
        var logPath = Path.Combine(_storeRoot, workflowId.ToString(), "logs.ndjson");
        if (!System.IO.File.Exists(logPath))
        {
            return NotFound(new { workflowId, message = "No logs found" });
        }

        var lines = System.IO.File.ReadAllLines(logPath, Encoding.UTF8);
        var entries = new List<object>();
        foreach (var line in lines)
        {
            try
            {
                var doc = JsonDocument.Parse(line);
                entries.Add(JsonSerializer.Deserialize<object>(line));
            }
            catch
            {
                // skip malformed lines
            }
        }
        return Ok(new { workflowId, entries });
    }
}