using CoreAxis.Modules.ApiManager.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.ApiManager.Presentation.Controllers;

[ApiController]
[Route("api/admin/apim/import")]
public class ImportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("openapi")]
    public async Task<ActionResult<ImportOpenApiResult>> ImportOpenApi([FromBody] ImportOpenApiRequest request)
    {
        var result = await _mediator.Send(new ImportOpenApiCommand(request.ServiceName, request.OpenApiSpec, request.BaseUrl));
        return Ok(result);
    }
}

public class ImportOpenApiRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string OpenApiSpec { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
