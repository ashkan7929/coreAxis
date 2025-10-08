using CoreAxis.Modules.MLMModule.Application.Commands;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreAxis.SharedKernel.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CoreAxis.Modules.MLMModule.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommissionRuleController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionRuleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new commission rule set
    /// </summary>
    [HttpPost]
    [RequirePermission("CommissionRules", "Create")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommissionRuleSetDto>> CreateCommissionRuleSet([FromBody] CreateCommissionRuleSetDto request)
    {
        var command = new CreateCommissionRuleSetCommand
        {
            Name = request.Name,
            Description = request.Description,
            MaxLevels = request.MaxLevels,
            MinimumPurchaseAmount = request.MinimumPurchaseAmount,
            RequireActiveUpline = request.RequireActiveUpline,
            CommissionLevels = request.CommissionLevels
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCommissionRuleSet), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get commission rule set by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionRuleSetDto>> GetCommissionRuleSet(Guid id)
    {
        var query = new GetCommissionRuleSetByIdQuery { RuleSetId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get default commission rule set
    /// </summary>
    [HttpGet("default")]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionRuleSetDto>> GetDefaultCommissionRuleSet()
    {
        var query = new GetDefaultCommissionRuleSetQuery();
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get commission rule set by product
    /// </summary>
    [HttpGet("product/{productId}")]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionRuleSetDto>> GetCommissionRuleSetByProduct(Guid productId)
    {
        var query = new GetCommissionRuleSetByProductQuery { ProductId = productId };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get active commission rule sets
    /// </summary>
    [HttpGet("active")]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionRuleSetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionRuleSetDto>>> GetActiveCommissionRuleSets()
    {
        var query = new GetActiveCommissionRuleSetsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all commission rule sets
    /// </summary>
    [HttpGet]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionRuleSetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionRuleSetDto>>> GetAllCommissionRuleSets()
    {
        var query = new GetAllCommissionRuleSetsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get product rule bindings
    /// </summary>
    [HttpGet("product-bindings")]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(IEnumerable<ProductRuleBindingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductRuleBindingDto>>> GetProductRuleBindings()
    {
        var query = new GetProductRuleBindingsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get product rule bindings by product
    /// </summary>
    [HttpGet("product-bindings/product/{productId}")]
    [RequirePermission("CommissionRules", "Read")]
    [ProducesResponseType(typeof(IEnumerable<ProductRuleBindingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductRuleBindingDto>>> GetProductRuleBindingsByProduct(Guid productId)
    {
        var query = new GetProductRuleBindingsByProductQuery { ProductId = productId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Update commission rule set
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("CommissionRules", "Update")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommissionRuleSetDto>> UpdateCommissionRuleSet(Guid id, [FromBody] UpdateCommissionRuleSetDto request)
    {
        var command = new UpdateCommissionRuleSetCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            MaxLevels = request.MaxLevels,
            MinimumPurchaseAmount = request.MinimumPurchaseAmount,
            RequireActiveUpline = request.RequireActiveUpline,
            IsActive = request.IsActive,
            CommissionLevels = request.CommissionLevels
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Activate commission rule set
    /// </summary>
    [HttpPost("{id}/activate")]
    [RequirePermission("CommissionRules", "Update")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommissionRuleSetDto>> ActivateCommissionRuleSet(Guid id)
    {
        var command = new ActivateCommissionRuleSetCommand { RuleSetId = id };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate commission rule set
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [RequirePermission("CommissionRules", "Update")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommissionRuleSetDto>> DeactivateCommissionRuleSet(Guid id)
    {
        var command = new DeactivateCommissionRuleSetCommand { RuleSetId = id };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Set default commission rule set
    /// </summary>
    [HttpPost("{id}/set-default")]
    [RequirePermission("CommissionRules", "Update")]
    [ProducesResponseType(typeof(CommissionRuleSetDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommissionRuleSetDto>> SetDefaultCommissionRuleSet(Guid id)
    {
        var command = new SetDefaultCommissionRuleSetCommand 
        { 
            RuleSetId = id
        };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete commission rule set
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("CommissionRules", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteCommissionRuleSet(Guid id)
    {
        var command = new DeleteCommissionRuleSetCommand { RuleSetId = id };
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Add product rule binding
    /// </summary>
    [HttpPost("product-bindings")]
    [RequirePermission("CommissionRules", "Create")]
    [ProducesResponseType(typeof(ProductRuleBindingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductRuleBindingDto>> AddProductRuleBinding([FromBody] CreateProductRuleBindingDto request)
    {
        var command = new AddProductRuleBindingCommand
        {
            ProductId = request.ProductId,
            CommissionRuleSetId = request.RuleSetId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProductRuleBindingsByProduct), new { productId = result.ProductId }, result);
    }


}