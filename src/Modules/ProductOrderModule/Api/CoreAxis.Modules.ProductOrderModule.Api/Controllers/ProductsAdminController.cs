using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.Products;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreAxis.Modules.ProductOrderModule.Application.Services;
using System.Net.Mime;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
public class ProductsAdminController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductsAdminController> _logger;
    private readonly IProductEventEmitter _productEventEmitter;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly IIdempotencyService _idempotencyService;

    public ProductsAdminController(IProductRepository productRepository, ILogger<ProductsAdminController> logger, IProductEventEmitter productEventEmitter, IValidator<CreateProductRequest> createValidator, IValidator<UpdateProductRequest> updateValidator, IIdempotencyService idempotencyService)
    {
        _productRepository = productRepository;
        _logger = logger;
        _productEventEmitter = productEventEmitter;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _idempotencyService = idempotencyService;
    }

    /// <summary>
    /// Get an admin view of a product by ID.
    /// </summary>
    /// <remarks>
    /// Example:
    ///
    /// GET `/api/admin/products/{id}`
    ///
    /// Responses:
    /// - 200 OK → returns full `ProductAdminDto`.
    /// - 404 NotFound → Problem+JSON with `code=PRODUCT_NOT_FOUND`.
    /// - 500 InternalServerError.
    /// </remarks>
    /// <param name="id">Product identifier (Guid).</param>
    [HttpGet("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // [HasPermission("Products", "Read")]
    public async Task<ActionResult<ProductAdminDto>> GetById(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Product not found",
                detail: $"No product with id {id}",
                statusCode: StatusCodes.Status404NotFound,
                code: "PRODUCT_NOT_FOUND");
        }
        TryAddCorrelationHeader();
        return Ok(MapToAdminDto(product));
    }

    /// <summary>
    /// Create a new product (admin).
    /// </summary>
    /// <remarks>
    /// Headers:
    /// - `Idempotency-Key` (optional GUID) to safely retry.
    ///
    /// Request body:
    /// ```json
    /// {
    ///   "code": "SKU-001",
    ///   "name": "Product 1",
    ///   "status": "Active",
    ///   "priceFrom": 99.99,
    ///   "currency": "USD",
    ///   "attributes": { "color": "blue" }
    /// }
    /// ```
    ///
    /// Responses:
    /// - 201 Created → `ProductAdminDto` with `Location` header to `GET /api/admin/products/{id}`.
    /// - 400 BadRequest → validation errors (Problem+JSON, `code=PRODUCT_INVALID`).
    /// - 409 Conflict → duplicate product code (Problem+JSON, `code=DUPLICATE_PRODUCT_CODE`).
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // [HasPermission("Products", "Write")]
    public async Task<ActionResult<ProductAdminDto>> Create([FromBody] CreateProductRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Invalid product",
                detail: string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                statusCode: StatusCodes.Status400BadRequest,
                code: "PRODUCT_INVALID",
                errors: validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToArray());
        }

        // Validate unique code
        var existing = await _productRepository.GetByCodeAsync(request.Code);
        var idempotencyKey = GetIdempotencyKey();
        var requestHashCreate = ComputeRequestHash(request);
        if (existing != null)
        {
            // Persistent idempotency: if key/hash indicate prior processing and payload matches, treat as created
            var processed = !string.IsNullOrEmpty(idempotencyKey)
                && await _idempotencyService.IsRequestProcessedAsync(idempotencyKey!, "product:create", requestHashCreate);
            if (processed && PayloadMatches(existing, request))
            {
                var dtoExisting = MapToAdminDto(existing);
                TryAddCorrelationHeader();
                Response.Headers["Location"] = Url.Action(nameof(GetById), new { id = existing.Id }) ?? string.Empty;
                return CreatedAtAction(nameof(GetById), new { id = existing.Id }, dtoExisting);
            }

            TryAddCorrelationHeader();
            Response.Headers["Location"] = Url.Action(nameof(GetById), new { id = existing.Id }) ?? string.Empty;
            return BuildProblem(
                title: "Duplicate code",
                detail: $"Product with code '{request.Code}' already exists.",
                statusCode: StatusCodes.Status409Conflict,
                code: "DUPLICATE_PRODUCT_CODE");
        }

        var money = request.PriceFrom.HasValue ? Money.Create(request.PriceFrom.Value, request.Currency ?? "USD") : null;
        var product = Product.Create(request.Code, request.Name, request.Status, money, request.Attributes, request.SupplierId);

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        // Record idempotency if header provided
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            await _idempotencyService.MarkRequestProcessedAsync(idempotencyKey!, "product:create", requestHashCreate);
        }

        var dto = MapToAdminDto(product);
        TryAddCorrelationHeader();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, dto);
    }

    /// <summary>
    /// Update an existing product (admin).
    /// </summary>
    /// <remarks>
    /// Headers:
    /// - `Idempotency-Key` (optional GUID) to safely retry.
    ///
    /// Responses:
    /// - 200 OK → updated `ProductAdminDto`.
    /// - 400 BadRequest → validation errors (Problem+JSON).
    /// - 404 NotFound → product not found.
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProductAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // [HasPermission("Products", "Write")]
    public async Task<ActionResult<ProductAdminDto>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Invalid product",
                detail: string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                statusCode: StatusCodes.Status400BadRequest,
                code: "PRODUCT_INVALID",
                errors: validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToArray());
        }

        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Product not found",
                detail: $"No product with id {id}",
                statusCode: StatusCodes.Status404NotFound,
                code: "PRODUCT_NOT_FOUND");
        }

        var previousStatus = product.Status;
        var money = request.PriceFrom.HasValue ? Money.Create(request.PriceFrom.Value, request.Currency ?? "USD") : null;

        // Idempotency shortcut: if no changes compared to current entity, return existing without emitting events
        if (UpdateIsNoOp(product, request))
        {
            TryAddCorrelationHeader();
            return Ok(MapToAdminDto(product));
        }

        product.Update(request.Name, request.Status, request.Count, request.Quantity, money, request.Attributes, request.SupplierId);

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        // Record idempotency if header provided
        var idempotencyKeyUpdate = GetIdempotencyKey();
        var requestHashUpdate = ComputeRequestHash(request);
        if (!string.IsNullOrEmpty(idempotencyKeyUpdate))
        {
            await _idempotencyService.MarkRequestProcessedAsync(idempotencyKeyUpdate!, $"product:update:{id}", requestHashUpdate);
        }

        // Emit integration events (optional via feature flag)
        var correlationId = GetCorrelationIdOrNew();
        await _productEventEmitter.EmitUpdatedAsync(product, correlationId);
        if (product.Status != previousStatus)
        {
            await _productEventEmitter.EmitStatusChangedAsync(product, product.Status.ToString(), correlationId);
        }

        TryAddCorrelationHeader();
        return Ok(MapToAdminDto(product));
    }

    /// <summary>
    /// Delete a product (admin).
    /// </summary>
    /// <remarks>
    /// Deletes/deactivates the product. Returns no content when successful.
    ///
    /// Responses:
    /// - 204 NoContent → deleted.
    /// - 404 NotFound → product not found.
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // [HasPermission("Products", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Product not found",
                detail: $"No product with id {id}",
                statusCode: StatusCodes.Status404NotFound,
                code: "PRODUCT_NOT_FOUND");
        }

        await _productRepository.DeleteAsync(product);
        await _productRepository.SaveChangesAsync();

        TryAddCorrelationHeader();
        return NoContent();
    }

    private static ProductAdminDto MapToAdminDto(Product product)
    {
        return new ProductAdminDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Status = product.Status,
            PriceFrom = product.PriceFrom?.Amount,
            Currency = product.PriceFrom?.Currency,
            Attributes = product.Attributes,
            SupplierId = product.SupplierId,
            CreatedOn = product.CreatedOn,
            LastModifiedOn = product.LastModifiedOn
        };
    }

    private void TryAddCorrelationHeader()
    {
        const string headerName = "X-Correlation-Id";
        if (Request.Headers.TryGetValue(headerName, out var value))
        {
            Response.Headers[headerName] = value.ToString();
        }
    }

    private Guid GetCorrelationIdOrNew()
    {
        const string headerName = "X-Correlation-Id";
        if (Request.Headers.TryGetValue(headerName, out var value) && Guid.TryParse(value, out var guid))
        {
            return guid;
        }
        return Guid.NewGuid();
    }

    private string? GetIdempotencyKey()
    {
        const string headerName = "Idempotency-Key";
        if (Request.Headers.TryGetValue(headerName, out var value))
        {
            return value.ToString();
        }
        return null;
    }

    private static string ComputeRequestHash(object request)
    {
        var json = JsonSerializer.Serialize(request);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static bool PayloadMatches(Product existing, CreateProductRequest request)
    {
        var priceAmount = request.PriceFrom;
        var priceCurrency = request.Currency ?? existing.PriceFrom?.Currency ?? "USD";
        return existing.Name == request.Name
            && existing.Status == request.Status
            && ((existing.PriceFrom == null && !priceAmount.HasValue) ||
                (existing.PriceFrom != null && priceAmount.HasValue && existing.PriceFrom.Amount == priceAmount.Value && existing.PriceFrom.Currency == priceCurrency))
            && DictionariesEqual(existing.Attributes, request.Attributes ?? new Dictionary<string, string>())
            && existing.SupplierId == request.SupplierId;
    }

    private static bool UpdateIsNoOp(Product existing, UpdateProductRequest request)
    {
        var priceAmount = request.PriceFrom;
        var priceCurrency = request.Currency ?? existing.PriceFrom?.Currency ?? "USD";
        return existing.Name == request.Name
            && existing.Status == request.Status
            && ((existing.PriceFrom == null && !priceAmount.HasValue) ||
                (existing.PriceFrom != null && priceAmount.HasValue && existing.PriceFrom.Amount == priceAmount.Value && existing.PriceFrom.Currency == priceCurrency))
            && DictionariesEqual(existing.Attributes, request.Attributes ?? new Dictionary<string, string>())
            && existing.SupplierId == request.SupplierId;
    }

    private static bool DictionariesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var val)) return false;
            if (!string.Equals(kv.Value, val, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private ObjectResult BuildProblem(string title, string detail, int statusCode, string code, object? errors = null)
    {
        var traceId = HttpContext.TraceIdentifier;
        var correlationId = Request.Headers.TryGetValue("X-Correlation-Id", out var corr) ? corr.ToString() : null;

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = "https://tools.ietf.org/html/rfc7807"
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(correlationId)) problem.Extensions["correlationId"] = correlationId;
        if (errors != null) problem.Extensions["errors"] = errors;

        var result = new ObjectResult(problem) { StatusCode = statusCode };
        Response.ContentType = "application/problem+json";
        return result;
    }
}