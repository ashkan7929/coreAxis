using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers;

/// <summary>
/// Controller for managing Formula Engine operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FormulaController : ControllerBase
{
    private readonly IFormulaService _formulaService;

    public FormulaController(IFormulaService formulaService)
    {
        _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
    }

    /// <summary>
    /// Evaluate a formula by ID
    /// </summary>
    /// <param name="formulaId">The formula ID</param>
    /// <param name="request">The evaluation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Formula evaluation result</returns>
    [HttpPost("{formulaId:guid}/evaluate")]
    public async Task<IActionResult> EvaluateFormula(
        [FromRoute] Guid formulaId,
        [FromBody] FormulaEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.EvaluateFormulaAsync(
            formulaId,
            request.Inputs,
            request.Context,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Evaluate a formula by name and version
    /// </summary>
    /// <param name="formulaName">The formula name</param>
    /// <param name="version">The formula version (optional, defaults to latest published)</param>
    /// <param name="request">The evaluation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Formula evaluation result</returns>
    [HttpPost("by-name/{formulaName}/evaluate")]
    public async Task<IActionResult> EvaluateFormulaByName(
        [FromRoute] string formulaName,
        [FromQuery] int? version,
        [FromBody] FormulaEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.EvaluateFormulaAsync(
            formulaName,
            version,
            request.Inputs,
            request.Context,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get the latest published version of a formula
    /// </summary>
    /// <param name="formulaId">The formula ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest published formula version</returns>
    [HttpGet("{formulaId:guid}/latest")]
    public async Task<IActionResult> GetLatestPublishedVersion(
        [FromRoute] Guid formulaId,
        CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.GetLatestPublishedVersionAsync(formulaId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Get available functions for formula expressions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available functions</returns>
    [HttpGet("functions")]
    public async Task<IActionResult> GetAvailableFunctions(CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.GetAvailableFunctionsAsync(cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Validate a formula expression
    /// </summary>
    /// <param name="request">The validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateExpression(
        [FromBody] FormulaValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.ValidateExpressionAsync(
            request.Expression,
            request.Context,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new { isValid = result.Value, message = "Expression is valid" });
        }

        return BadRequest(new { isValid = false, error = result.Error });
    }

    /// <summary>
    /// Get evaluation history for a formula
    /// </summary>
    /// <param name="formulaId">The formula ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated evaluation history</returns>
    [HttpGet("{formulaId:guid}/history")]
    public async Task<IActionResult> GetEvaluationHistory(
        [FromRoute] Guid formulaId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.GetEvaluationHistoryAsync(
            formulaId,
            pageNumber,
            pageSize,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get performance metrics for a formula
    /// </summary>
    /// <param name="formulaId">The formula ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Formula performance metrics</returns>
    [HttpGet("{formulaId:guid}/metrics")]
    public async Task<IActionResult> GetPerformanceMetrics(
        [FromRoute] Guid formulaId,
        CancellationToken cancellationToken = default)
    {
        var result = await _formulaService.GetPerformanceMetricsAsync(formulaId, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new { error = result.Error });
    }
}

/// <summary>
/// Request model for formula evaluation
/// </summary>
public class FormulaEvaluationRequest
{
    /// <summary>
    /// Input variables for the formula
    /// </summary>
    [Required]
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Evaluation context (optional)
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Request model for formula validation
/// </summary>
public class FormulaValidationRequest
{
    /// <summary>
    /// The formula expression to validate
    /// </summary>
    [Required]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Validation context (optional)
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}