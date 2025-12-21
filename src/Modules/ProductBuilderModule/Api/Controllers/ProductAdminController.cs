using CoreAxis.Modules.ProductBuilderModule.Application.Commands;
using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoreAxis.Modules.AuthModule.API.Authz;

namespace CoreAxis.Modules.ProductBuilderModule.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize]
public class ProductAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [HasPermission("ProductBuilder", "Create")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        var result = await _mediator.Send(new CreateProductCommand(dto));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpGet]
    [HasPermission("ProductBuilder", "Read")]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _mediator.Send(new GetProductsQuery());
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [HasPermission("ProductBuilder", "Read")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductQuery(id));
        if (!result.IsSuccess) return NotFound(new { errors = result.Errors });
        return Ok(result.Value);
    }

    [HttpPost("{id}/versions")]
    [HasPermission("ProductBuilder", "Edit")]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateVersionDto dto)
    {
        var result = await _mediator.Send(new CreateVersionCommand(id, dto));
        if (!result.IsSuccess) return BadRequest(new { errors = result.Errors });
        return Ok(result.Value);
    }
}
