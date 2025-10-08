using CoreAxis.Modules.ApiManager.Application.Queries;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/admin/apim/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IMediator mediator, ILogger<LogsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get API call logs with optional filtering
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    /// GET /api/admin/apim/logs?webServiceId={id}&methodId={id}&succeeded=true&fromDate=2025-01-01&toDate=2025-01-31&pageNumber=1&pageSize=50
    ///
    /// Query parameters:
    /// - webServiceId: Filter by web service ID.
    /// - methodId: Filter by method ID.
    /// - succeeded: Filter by success status.
    /// - fromDate/toDate: Filter by date range (UTC).
    /// - pageNumber/pageSize: Pagination controls.
    ///
    /// Responses:
    /// - 200: Returns paged call logs.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Read permission).
    /// - 500: Internal error.
    /// </remarks>
    [HttpGet]
    [HasPermission("ApiManager", "Read")]
    [ProducesResponseType(typeof(GetCallLogsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCallLogsResult>> GetCallLogs(
        [FromQuery] Guid? webServiceId = null,
        [FromQuery] Guid? methodId = null,
        [FromQuery] bool? succeeded = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetCallLogsQuery(
                webServiceId,
                methodId,
                succeeded,
                fromDate,
                toDate,
                pageNumber,
                pageSize
            );

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving call logs");
            var problem = new ProblemDetails
            {
                Type = "https://coreaxis.dev/problems/api-manager/internal-error",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Internal server error"
            };
            problem.Extensions["code"] = "api_manager.internal_error";
            return StatusCode(StatusCodes.Status500InternalServerError, problem);
        }
    }
}