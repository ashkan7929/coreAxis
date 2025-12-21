using CoreAxis.Modules.ProductBuilderModule.Application.Commands;
using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoreAxis.Modules.AuthModule.API.Authz;

namespace CoreAxis.Modules.ProductBuilderModule.Api.Controllers;

[ApiController]
[Route("api/admin/product-versions")]
[Authorize]
public class ProductVersionAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductVersionAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{versionId}")]
    [HasPermission("ProductBuilder", "Edit")]
    public async Task<IActionResult> UpdateVersion(Guid versionId, [FromBody] UpdateVersionDto dto)
    {
        var result = await _mediator.Send(new UpdateVersionCommand(versionId, dto));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("{versionId}/validate")]
    [HasPermission("ProductBuilder", "Read")]
    public async Task<IActionResult> ValidateVersion(Guid versionId)
    {
        var result = await _mediator.Send(new ValidateVersionCommand(versionId));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("{versionId}/publish")]
    [HasPermission("ProductBuilder", "Publish")]
    public async Task<IActionResult> PublishVersion(Guid versionId)
    {
        var result = await _mediator.Send(new PublishProductVersionCommand(versionId));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpGet("{versionId}/dependencies")]
    [HasPermission("ProductBuilder", "Read")]
    public async Task<IActionResult> GetDependencies(Guid versionId)
    {
        var result = await _mediator.Send(new GetProductDependenciesQuery(versionId));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }
}
