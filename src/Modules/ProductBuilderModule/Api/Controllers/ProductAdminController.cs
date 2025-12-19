using CoreAxis.Modules.ProductBuilderModule.Application.Commands;
using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.ProductBuilderModule.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
public class ProductAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        var result = await _mediator.Send(new CreateProductCommand(dto));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _mediator.Send(new GetProductsQuery());
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductQuery(id));
        if (!result.IsSuccess) return NotFound(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("{id}/versions")]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateVersionDto dto)
    {
        var result = await _mediator.Send(new CreateVersionCommand(id, dto));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }
}

[ApiController]
[Route("api/admin/product-versions")]
public class ProductVersionAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductVersionAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{versionId}")]
    public async Task<IActionResult> UpdateVersion(Guid versionId, [FromBody] UpdateVersionDto dto)
    {
        var result = await _mediator.Send(new UpdateVersionCommand(versionId, dto));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("{versionId}/validate")]
    public async Task<IActionResult> ValidateVersion(Guid versionId)
    {
        var result = await _mediator.Send(new ValidateVersionCommand(versionId));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("{versionId}/publish")]
    public async Task<IActionResult> PublishVersion(Guid versionId)
    {
        var result = await _mediator.Send(new PublishProductVersionCommand(versionId));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpGet("{versionId}/dependencies")]
    public async Task<IActionResult> GetDependencies(Guid versionId)
    {
        var result = await _mediator.Send(new GetProductDependenciesQuery(versionId));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }
}
