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
    [HttpGet]
    [HasPermission("ApiManager", "Read")]
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