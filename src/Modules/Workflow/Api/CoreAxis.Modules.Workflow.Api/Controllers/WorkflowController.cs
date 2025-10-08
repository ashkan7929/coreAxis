using CoreAxis.SharedKernel.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;

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

    /// <summary>
    /// Start the post-finalize workflow for an order.
    /// </summary>
    /// <remarks>
    /// Triggers the workflow named `post-finalize-workflow` with the provided context.
    ///
    /// Request body example:
    /// ```json
    /// {
    ///   "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "userId": "59e3c5f1-7f2d-4b22-9e8f-5c7c4e5b2a10",
    ///   "totalAmount": 149.99,
    ///   "currency": "USD",
    ///   "finalizedAt": "2024-01-01T10:00:00Z",
    ///   "tenantId": "coreaxis",
    ///   "correlationId": "c2c2b2e2-..."
    /// }
    /// ```
    ///
    /// Responses:
    /// - 200 OK → workflow start result `{ workflowId, isSuccess, error? }`
    /// - 400 BadRequest → invalid payload
    /// - 401 Unauthorized → missing or invalid auth
    /// - 500 InternalServerError
    /// </remarks>
    /// <param name="context">Workflow context payload as JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("post-finalize/start")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartPostFinalize([FromBody] JsonElement context, CancellationToken cancellationToken)
    {
        var result = await _workflowClient.StartAsync("post-finalize-workflow", context, cancellationToken);
        _logger.LogInformation("Started post-finalize workflow {WorkflowId}", result.WorkflowId);
        return Ok(result);
    }

    /// <summary>
    /// Resume a paused workflow.
    /// </summary>
    /// <remarks>
    /// Signals the workflow engine with `Resume` for the given `workflowId`.
    ///
    /// Responses:
    /// - 200 OK → resume signal result
    /// - 404 NotFound → workflow not found
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    /// <param name="workflowId">Target workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{workflowId:guid}/resume")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Resume(Guid workflowId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { workflowId }));
        var result = await _workflowClient.SignalAsync("Resume", payload, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a running workflow.
    /// </summary>
    /// <remarks>
    /// Sends a `Cancel` signal to the workflow.
    ///
    /// Responses:
    /// - 200 OK → cancel signal result
    /// - 404 NotFound → workflow not found
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    /// <param name="workflowId">Target workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{workflowId:guid}/cancel")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(Guid workflowId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { workflowId }));
        var result = await _workflowClient.SignalAsync("Cancel", payload, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get workflow status by ID.
    /// </summary>
    /// <remarks>
    /// Returns detailed status from workflow engine.
    ///
    /// Responses:
    /// - 200 OK → status payload
    /// - 404 NotFound → workflow not found
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{workflowId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus(Guid workflowId, CancellationToken cancellationToken)
    {
        var result = await _workflowClient.GetWorkflowStatusAsync(workflowId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get workflow execution history (logs).
    /// </summary>
    /// <remarks>
    /// Reads NDJSON logs from `App_Data/workflows/{workflowId}/logs.ndjson` if present.
    ///
    /// Responses:
    /// - 200 OK → `{ workflowId, entries: [...] }`
    /// - 404 NotFound → no logs found
    /// - 401 Unauthorized
    /// - 500 InternalServerError
    /// </remarks>
    /// <param name="workflowId">Workflow identifier.</param>
    [HttpGet("{workflowId:guid}/history")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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