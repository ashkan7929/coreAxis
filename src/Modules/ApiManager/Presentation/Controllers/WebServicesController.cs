using CoreAxis.Modules.ApiManager.Application.Commands;
using CoreAxis.Modules.ApiManager.Application.Queries;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WebServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebServicesController> _logger;

    public WebServicesController(IMediator mediator, ILogger<WebServicesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all web services with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [HasPermission("ApiManager", "Read")]
    public async Task<ActionResult<GetWebServicesResult>> GetWebServices(
        [FromQuery] string? ownerTenantId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetWebServicesQuery(ownerTenantId, isActive, pageNumber, pageSize);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving web services");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get web service details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("ApiManager", "Read")]
    public async Task<ActionResult<WebServiceDetailsDto>> GetWebService(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetWebServiceDetailsQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
            {
                return NotFound(new { message = $"WebService with ID {id} not found" });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving web service {WebServiceId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new web service
    /// </summary>
    [HttpPost]
    [HasPermission("ApiManager", "Create")]
    public async Task<ActionResult<CreateWebServiceResponse>> CreateWebService(
        [FromBody] CreateWebServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new CreateWebServiceCommand(
                request.Name,
                request.Description,
                request.BaseUrl,
                request.SecurityProfileId,
                request.OwnerTenantId
            );
            
            var webServiceId = await _mediator.Send(command, cancellationToken);
            
            var response = new CreateWebServiceResponse(webServiceId);
            return CreatedAtAction(nameof(GetWebService), new { id = webServiceId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating web service");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for creating web service");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating web service");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new method for a web service
    /// </summary>
    [HttpPost("{webServiceId:guid}/methods")]
    [HasPermission("ApiManager", "Create")]
    public async Task<ActionResult<CreateWebServiceMethodResponse>> CreateWebServiceMethod(
        Guid webServiceId,
        [FromBody] CreateWebServiceMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new CreateWebServiceMethodCommand(
                webServiceId,
                request.Name,
                request.Description,
                request.Path,
                request.HttpMethod,
                request.TimeoutMs,
                request.RetryPolicyJson,
                request.CircuitPolicyJson,
                request.Parameters?.Select(p => new Application.Commands.CreateWebServiceParamDto(
                    p.Name,
                    p.Description,
                    p.Location,
                    p.DataType,
                    p.IsRequired,
                    p.DefaultValue
                )).ToList()
            );
            
            var methodId = await _mediator.Send(command, cancellationToken);
            
            var response = new CreateWebServiceMethodResponse(methodId);
            return CreatedAtAction(nameof(GetWebService), new { id = webServiceId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating web service method");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for creating web service method");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating web service method for WebService {WebServiceId}", webServiceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Test/invoke a web service method
    /// </summary>
    [HttpPost("methods/{methodId:guid}/invoke")]
    [HasPermission("ApiManager", "Execute")]
    public async Task<ActionResult<InvokeApiMethodResponse>> InvokeMethod(
        Guid methodId,
        [FromBody] InvokeApiMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new InvokeApiMethodCommand(methodId, request.Parameters);
            var result = await _mediator.Send(command, cancellationToken);
            
            var response = new InvokeApiMethodResponse(
                result.IsSuccess,
                result.StatusCode,
                result.ResponseBody,
                result.ErrorMessage,
                result.LatencyMs,
                result.ResponseHeaders
            );
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for invoking method {MethodId}", methodId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking method {MethodId}", methodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get call logs with optional filtering
    /// </summary>
    [HttpGet("call-logs")]
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
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a web service
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission("ApiManager", "Update")]
    public async Task<ActionResult> UpdateWebService(
        Guid id,
        [FromBody] UpdateWebServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UpdateWebServiceCommand(
                id,
                request.Name,
                request.BaseUrl,
                request.Description,
                request.SecurityProfileId
            );
            
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return NotFound(new { message = $"WebService with ID {id} not found" });
            }
            
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating web service {WebServiceId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for updating web service {WebServiceId}", id);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating web service {WebServiceId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a web service
    /// </summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("ApiManager", "Delete")]
    public async Task<ActionResult> DeleteWebService(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteWebServiceCommand(id);
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return NotFound(new { message = $"WebService with ID {id} not found" });
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting web service {WebServiceId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get methods for a specific web service
    /// </summary>
    [HttpGet("{webServiceId:guid}/methods")]
    [HasPermission("ApiManager", "Read")]
    public async Task<ActionResult<List<WebServiceMethodDto>>> GetWebServiceMethods(
        Guid webServiceId,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? httpMethod = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetWebServiceMethodsQuery(webServiceId, isActive, httpMethod);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving methods for web service {WebServiceId}", webServiceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a web service method
    /// </summary>
    [HttpPut("methods/{methodId:guid}")]
    [HasPermission("ApiManager", "Update")]
    public async Task<ActionResult> UpdateWebServiceMethod(
        Guid methodId,
        [FromBody] UpdateWebServiceMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new UpdateWebServiceMethodCommand(
                methodId,
                request.Path,
                request.HttpMethod,
                request.TimeoutMs,
                null, // RequestSchema
                null, // ResponseSchema
                request.RetryPolicyJson,
                request.CircuitPolicyJson
            );
            
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return NotFound(new { message = $"Method with ID {methodId} not found" });
            }
            
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating method {MethodId}", methodId);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for updating method {MethodId}", methodId);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating method {MethodId}", methodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a web service method
    /// </summary>
    [HttpDelete("methods/{methodId:guid}")]
    [HasPermission("ApiManager", "Delete")]
    public async Task<ActionResult> DeleteWebServiceMethod(
        Guid methodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteWebServiceMethodCommand(methodId);
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
            {
                return NotFound(new { message = $"Method with ID {methodId} not found" });
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting method {MethodId}", methodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

// Request/Response DTOs
public record CreateWebServiceRequest(
    string Name,
    string Description,
    string BaseUrl,
    Guid? SecurityProfileId = null,
    string? OwnerTenantId = null
);

public record CreateWebServiceResponse(Guid Id);

public record CreateWebServiceMethodRequest(
    string Name,
    string Description,
    string Path,
    string HttpMethod,
    int TimeoutMs = 30000,
    string? RetryPolicyJson = null,
    string? CircuitPolicyJson = null,
    List<CreateWebServiceParamRequest>? Parameters = null
);

public record CreateWebServiceParamRequest(
    string Name,
    string Description,
    CoreAxis.Modules.ApiManager.Domain.ParameterLocation Location,
    string DataType,
    bool IsRequired = false,
    string? DefaultValue = null
);

public record CreateWebServiceMethodResponse(Guid Id);

public record InvokeApiMethodRequest(
    Dictionary<string, object> Parameters
);

public record InvokeApiMethodResponse(
    bool IsSuccess,
    int? StatusCode,
    string? ResponseBody,
    string? ErrorMessage,
    long LatencyMs,
    Dictionary<string, string>? Headers
);

public record UpdateWebServiceRequest(
    string Name,
    string BaseUrl,
    string? Description = null,
    Guid? SecurityProfileId = null
);

public record UpdateWebServiceMethodRequest(
    string Name,
    string Description,
    string Path,
    string HttpMethod,
    int TimeoutMs = 30000,
    string? RetryPolicyJson = null,
    string? CircuitPolicyJson = null
);