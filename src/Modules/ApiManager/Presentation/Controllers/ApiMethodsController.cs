using CoreAxis.Modules.ApiManager.Application.Commands;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/admin/apim/methods")]
[Authorize]
public class ApiMethodsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ApiMethodsController> _logger;

    public ApiMethodsController(IMediator mediator, ILogger<ApiMethodsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Test an API method with provided parameters.
    /// </summary>
    /// <param name="id">The method ID.</param>
    /// <param name="request">The test request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test result including response and logs.</returns>
    [HttpPost("{id:guid}/test")]
    [HasPermission("ApiManager", "Read")] // Assuming Read permission is enough to test, or maybe Write? "Read" is safer for now.
    [ProducesResponseType(typeof(TestApiMethodResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestApiMethodResult>> TestMethod(
        Guid id,
        [FromBody] TestApiMethodRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new TestApiMethodCommand(id, request.Parameters);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success && result.ValidationErrors.Contains("Method not found"))
            {
                return NotFound(new ProblemDetails 
                { 
                    Title = "Method Not Found", 
                    Detail = $"API Method with ID {id} was not found." 
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API method {MethodId}", id);
            return Problem(
                title: "Internal Server Error",
                detail: "Failed to execute API method test.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

public class TestApiMethodRequestDto
{
    public Dictionary<string, object> Parameters { get; set; } = new();
}
