using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Products;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<PagedResult<ProductPublicDto>>> Get(
        [FromQuery] string? q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Public listing must only show Active products
        var status = ProductStatus.Active;

        var items = await _productRepository.GetLightweightAsync(status, q, pageNumber, pageSize);
        var total = await _productRepository.GetLightweightCountAsync(status, q);

        var dtoItems = items.Select(p => new ProductPublicDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Status = p.Status,
            PriceFrom = p.PriceFrom,
            Attributes = p.Attributes
        }).ToList();

        var result = new PagedResult<ProductPublicDto>
        {
            Items = dtoItems,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };

        TryAddCorrelationHeader();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ProductPublicDto>> GetById(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Product not found",
                detail: $"No product with id '{id}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "PRODUCT_NOT_FOUND");
        }

        var dto = new ProductPublicDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Status = product.Status,
            PriceFrom = product.PriceFrom,
            Attributes = product.Attributes
        };

        TryAddCorrelationHeader();
        return Ok(dto);
    }

    [HttpGet("by-code/{code}")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ProductPublicDto>> GetByCode(string code)
    {
        var product = await _productRepository.GetByCodeAsync(code);
        if (product == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Product not found",
                detail: $"No product with code '{code}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "PRODUCT_NOT_FOUND");
        }

        var dto = new ProductPublicDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Status = product.Status,
            PriceFrom = product.PriceFrom,
            Attributes = product.Attributes
        };

        TryAddCorrelationHeader();
        return Ok(dto);
    }

    private void TryAddCorrelationHeader()
    {
        const string headerName = "X-Correlation-Id";
        if (Request.Headers.TryGetValue(headerName, out var value))
        {
            Response.Headers[headerName] = value.ToString();
        }
    }

    private ActionResult BuildProblem(string title, string detail, int statusCode, string code, object? errors = null)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var correlationId = Request.Headers.TryGetValue("X-Correlation-Id", out var cid) ? cid.ToString() : null;

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = "https://coreaxis.dev/problems/product",
            Instance = HttpContext.Request.Path
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }
        if (errors != null)
        {
            problem.Extensions["errors"] = JsonSerializer.SerializeToElement(errors);
        }

        Response.ContentType = MediaTypeNames.Application.Json;
        return StatusCode(statusCode, problem);
    }
}