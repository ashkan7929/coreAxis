using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Suppliers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierRepository _supplierRepository;

    public SuppliersController(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    /// <summary>
    /// Create a new supplier.
    /// </summary>
    /// <remarks>
    /// Request body:
    /// {
    ///   "code": "SUP-001",
    ///   "name": "Acme Corp"
    /// }
    /// 
    /// Responses:
    /// - 201 Created → SupplierDto with Location header to GET /api/suppliers/{id}
    /// - 400 BadRequest → validation errors (Problem+JSON, code=SUPPLIER_INVALID)
    /// - 409 Conflict → duplicate code or name (Problem+JSON)
    /// </remarks>
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Invalid supplier",
                detail: "Code and Name are required",
                statusCode: StatusCodes.Status400BadRequest,
                code: "SUPPLIER_INVALID");
        }

        // Uniqueness checks
        var existingByCode = await _supplierRepository.GetByCodeAsync(request.Code.Trim());
        if (existingByCode != null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Duplicate code",
                detail: $"Supplier with code '{request.Code}' already exists.",
                statusCode: StatusCodes.Status409Conflict,
                code: "DUPLICATE_SUPPLIER_CODE");
        }
        var existingByName = await _supplierRepository.GetByNameAsync(request.Name.Trim());
        if (existingByName != null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Duplicate name",
                detail: $"Supplier with name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict,
                code: "DUPLICATE_SUPPLIER_NAME");
        }

        var supplier = Supplier.Create(request.Code, request.Name);
        await _supplierRepository.AddAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        var dto = MapToDto(supplier);
        TryAddCorrelationHeader();
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, dto);
    }

    /// <summary>
    /// Get suppliers with optional search and paging.
    /// </summary>
    /// <param name="q">Search term (code or name contains).</param>
    /// <param name="isActive">Filter by active status.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<SupplierDto>>> Get(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var items = await _supplierRepository.GetAllAsync(isActive, q, page, pageSize);
        var total = await _supplierRepository.GetAllCountAsync(isActive, q);

        var dtoItems = items.Select(MapToDto).ToList();

        var result = new PagedResult<SupplierDto>
        {
            Items = dtoItems,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };

        TryAddCorrelationHeader();
        return Ok(result);
    }

    /// <summary>
    /// Get a supplier by ID.
    /// </summary>
    /// <param name="id">Supplier identifier (Guid).</param>
    [HttpGet("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Supplier not found",
                detail: $"No supplier with id '{id}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "SUPPLIER_NOT_FOUND");
        }

        TryAddCorrelationHeader();
        return Ok(MapToDto(supplier));
    }

    /// <summary>
    /// Update a supplier's name.
    /// </summary>
    /// <param name="id">Supplier identifier.</param>
    /// <param name="request">Update payload.</param>
    [HttpPut("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Invalid supplier",
                detail: "Name is required",
                statusCode: StatusCodes.Status400BadRequest,
                code: "SUPPLIER_INVALID");
        }

        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Supplier not found",
                detail: $"No supplier with id '{id}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "SUPPLIER_NOT_FOUND");
        }

        var existingByName = await _supplierRepository.GetByNameAsync(request.Name.Trim());
        if (existingByName != null && existingByName.Id != id)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Duplicate name",
                detail: $"Supplier with name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict,
                code: "DUPLICATE_SUPPLIER_NAME");
        }

        // No-op check
        if (supplier.Name == request.Name.Trim())
        {
            TryAddCorrelationHeader();
            return Ok(MapToDto(supplier));
        }

        supplier.Update(request.Name);
        await _supplierRepository.UpdateAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        TryAddCorrelationHeader();
        return Ok(MapToDto(supplier));
    }

    /// <summary>
    /// Delete (deactivate) a supplier.
    /// </summary>
    /// <param name="id">Supplier identifier.</param>
    [HttpDelete("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            TryAddCorrelationHeader();
            return BuildProblem(
                title: "Supplier not found",
                detail: $"No supplier with id '{id}'",
                statusCode: StatusCodes.Status404NotFound,
                code: "SUPPLIER_NOT_FOUND");
        }

        await _supplierRepository.DeleteAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        TryAddCorrelationHeader();
        return NoContent();
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            Code = supplier.Code,
            Name = supplier.Name,
            IsActive = supplier.IsActive,
            CreatedOn = supplier.CreatedOn,
            LastModifiedOn = supplier.LastModifiedOn
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

    private ActionResult BuildProblem(string title, string detail, int statusCode, string code, object? errors = null)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var correlationId = Request.Headers.TryGetValue("X-Correlation-Id", out var cid) ? cid.ToString() : null;

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = "https://coreaxis.dev/problems/supplier",
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