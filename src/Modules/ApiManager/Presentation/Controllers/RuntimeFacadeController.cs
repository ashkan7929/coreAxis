using CoreAxis.Modules.ApiManager.Application.Commands;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("apim")]
[Authorize]
public class RuntimeFacadeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RuntimeFacadeController> _logger;

    public RuntimeFacadeController(IMediator mediator, ILogger<RuntimeFacadeController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public class RuntimeCallRequest
    {
        public Guid? MethodId { get; set; }
        public string? ServiceName { get; set; }
        public string? HttpMethod { get; set; }
        public string? Path { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    [HttpPost("call")]
    [EnableRateLimiting("apim-call")]
    [HasPermission("ApiManager", "Execute")]
    /// <summary>
    /// Invoke downstream API via runtime facade using either MethodId or (ServiceName, HttpMethod, Path).
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    /// {
    ///   "serviceName": "PricingService",
    ///   "httpMethod": "GET",
    ///   "path": "/quotes",
    ///   "parameters": { "symbol": "EURUSD" }
    /// }
    ///
    /// or
    ///
    /// { "methodId": "00000000-0000-0000-0000-000000000000", "parameters": { "symbol": "EURUSD" } }
    ///
    /// Responses:
    /// - 200: Downstream success; returns downstream body and status code.
    /// - 400: Invalid request body or missing arguments.
    /// - 401: Unauthorized.
    /// - 403: Forbidden (missing ApiManager Execute permission).
    /// - 502: Downstream error mapped to Bad Gateway.
    /// - 500: Internal error.
    /// </remarks>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Call([FromBody] RuntimeCallRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://coreaxis.dev/problems/api-manager/invalid-request",
                Title = "Invalid Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required"
            });
        }

        var parameters = request.Parameters ?? new Dictionary<string, object>();

        try
        {
            CoreAxis.Modules.ApiManager.Application.Contracts.ApiProxyResult result;
            if (request.MethodId.HasValue)
            {
                result = await _mediator.Send(new InvokeApiMethodCommand(request.MethodId.Value, parameters), cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ServiceName) || string.IsNullOrWhiteSpace(request.HttpMethod) || string.IsNullOrWhiteSpace(request.Path))
                {
                    var problem = new ProblemDetails
                    {
                        Type = "https://coreaxis.dev/problems/api-manager/invalid-argument",
                        Title = "Invalid Argument",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "Either MethodId or (ServiceName + HttpMethod + Path) must be provided"
                    };
                    problem.Extensions["code"] = "api_manager.runtime.invalid_arguments";
                    return BadRequest(problem);
                }

                result = await _mediator.Send(new InvokeApiEndpointCommand(
                    request.ServiceName!, request.HttpMethod!, request.Path!, parameters), cancellationToken);
            }

            if (result.IsSuccess)
            {
                // Pass-through downstream response status code and body
                var status = result.StatusCode ?? StatusCodes.Status200OK;
                var body = result.ResponseBody ?? string.Empty;
                return StatusCode(status, body);
            }
            else
            {
                var status = result.StatusCode ?? StatusCodes.Status502BadGateway;
                var problem = new ProblemDetails
                {
                    Type = "https://coreaxis.dev/problems/api-manager/downstream-error",
                    Title = "Downstream Error",
                    Status = status,
                    Detail = result.ErrorMessage ?? "Unexpected error"
                };
                problem.Extensions["code"] = "api_manager.runtime.downstream_error";
                return StatusCode(status, problem);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Runtime facade call failed");
            var problem = new ProblemDetails
            {
                Type = "https://coreaxis.dev/problems/api-manager/runtime-error",
                Title = "Runtime Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message
            };
            problem.Extensions["code"] = "api_manager.runtime.error";
            return StatusCode(StatusCodes.Status500InternalServerError, problem);
        }
    }
}