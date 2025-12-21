using CoreAxis.Modules.ProductBuilderModule.Application.Commands;
using CoreAxis.Modules.ProductBuilderModule.Application.DTOs;
using CoreAxis.Modules.ProductBuilderModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CoreAxis.Modules.ProductBuilderModule.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductRuntimeController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductRuntimeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a product by its key.
    /// </summary>
    [HttpGet("{productKey}")]
    public async Task<IActionResult> GetProduct(string productKey)
    {
        var result = await _mediator.Send(new GetProductByKeyQuery(productKey));
        if (!result.IsSuccess) return NotFound(new { errors = result.Errors });
        return Ok(result.Value);
    }

    /// <summary>
    /// Starts a product instance (workflow).
    /// </summary>
    [HttpPost("{productKey}/start")]
    public async Task<IActionResult> StartProduct(string productKey, [FromBody] JsonElement? context)
    {
        var command = new StartProductCommand(productKey, context);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a quote for a product (pricing).
    /// </summary>
    [HttpPost("{productKey}/quote")]
    public async Task<IActionResult> QuoteProduct(string productKey, [FromBody] JsonElement inputs)
    {
        var command = new QuoteProductCommand(productKey, inputs);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }
        
        return Ok(result.Value);
    }
}
