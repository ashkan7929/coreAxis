using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Application.Queries.ThirdParty;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.Products;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IMediator _mediator;

    public ProductsController(IProductRepository productRepository, IMediator mediator)
    {
        _productRepository = productRepository;
        _mediator = mediator;
    }
    /// <summary>
    /// Retrieve public product list with optional search and pagination.
    /// </summary>
    /// <remarks>
    /// Returns only products with status `Active`.
    ///
    /// Example response (paged):
    ///
    /// ```json
    /// {
    ///   "items": [
    ///     {
    ///       "id": "b1b2c3d4-0000-0000-0000-000000000001",
    ///       "code": "PRD-001",
    ///       "name": "Starter Pack",
    ///       "status": "Active",
    ///       "priceFrom": 49.99,
    ///       "attributes": { "color": "red" }
    ///     }
    ///   ],
    ///   "totalCount": 1,
    ///   "pageNumber": 1,
    ///   "pageSize": 20,
    ///   "totalPages": 1
    /// }
    /// ```
    /// </remarks>
    /// <param name="q">Optional search query by name/code.</param>
    /// <param name="pageNumber">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(PagedResult<ProductPublicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ProductPublicDto>>> Get(
        [FromQuery] string? q,
        [FromQuery] Guid? supplierId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Public listing must only show Active products
        var status = ProductStatus.Active;

        var items = await _productRepository.GetLightweightAsync(status, q, supplierId, pageNumber, pageSize);
        var total = await _productRepository.GetLightweightCountAsync(status, q, supplierId);

        var dtoItems = items.Select(p => new ProductPublicDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Status = p.Status,
            Count = p.Count,
            Quantity = p.Quantity,
            PriceFrom = p.PriceFrom,
            Attributes = p.Attributes
        }).ToList();

        #region Get Price From Mili
        try
        {
            var milliResult = await _mediator.Send(new GetMilliPriceQuery());

            if (milliResult != null && milliResult.Data != null)
            {
                foreach(var item in dtoItems)
                {
                    milliResult.Data.Price18 = (milliResult.Data.Price18 * 0.1m);
                    item.PriceFrom = new Domain.ValueObjects.Money(milliResult.Data.Price18 * 1000, "IRR");
                }
            }
        }
        catch (Exception)
        {

            throw;
        }
        #endregion
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

    /// <summary>
    /// Retrieve a public product by ID.
    /// </summary>
    /// <remarks>
    /// Returns `404` with problem details when the product is not found.
    ///
    /// Example success:
    ///
    /// ```json
    /// {
    ///   "id": "b1b2c3d4-0000-0000-0000-000000000001",
    ///   "code": "PRD-001",
    ///   "name": "Starter Pack",
    ///   "status": "Active",
    ///   "priceFrom": 49.99,
    ///   "attributes": { "color": "red" }
    /// }
    /// ```
    ///
    /// Example not found (ProblemDetails):
    ///
    /// ```json
    /// {
    ///   "title": "Product not found",
    ///   "detail": "No product with id '...'",
    ///   "status": 404,
    ///   "type": "https://coreaxis.dev/problems/product"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">Product unique identifier.</param>
    [HttpGet("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProductPublicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            Count = product.Count,
            Quantity = product.Quantity,
            PriceFrom = product.PriceFrom,
            Attributes = product.Attributes
        };
        #region Get Price From Mili
        try
        {
            var result = await _mediator.Send(new GetMilliPriceQuery());

            if (result != null && result.Data != null)
            {
                result.Data.Price18 = (result.Data.Price18 * 0.1m);
                dto.PriceFrom = new Domain.ValueObjects.Money(result.Data.Price18 * 1000, "IRR");
            }
        }
        catch (Exception)
        {

            throw;
        }
        #endregion
        TryAddCorrelationHeader();
        return Ok(dto);
    }

    /// <summary>
    /// Retrieve a public product by code.
    /// </summary>
    /// <remarks>
    /// Returns `404` with problem details when the product is not found.
    /// </remarks>
    /// <param name="code">Product code (case-sensitive).</param>
    [HttpGet("by-code/{code}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProductPublicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            Count = product.Count,
            Quantity = product.Quantity,
            Status = product.Status,
            PriceFrom = product.PriceFrom,
            Attributes = product.Attributes
        };
        #region Get Price From Mili
        try
        {
            var result = await _mediator.Send(new GetMilliPriceQuery());

            if (result != null && result.Data != null)
            {
                dto.PriceFrom = new Domain.ValueObjects.Money(result.Data.Price18 * 1000, "IRR");
            }
        }
        catch (Exception)
        {

            throw;
        }
        #endregion
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