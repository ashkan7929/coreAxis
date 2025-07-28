using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Queries;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.WalletModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
    {
        var query = new GetTransactionByIdQuery { TransactionId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get transactions with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromQuery] Guid? walletId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? transactionTypeId = null,
        [FromQuery] TransactionStatus? status = null)
    {
        var tenantId = GetTenantId();
        var query = new GetTransactionsQuery
        {
            Filter = new TransactionFilterDto
            {
                WalletId = walletId,
                UserId = userId,
                FromDate = startDate,
                ToDate = endDate,
                TransactionTypeId = transactionTypeId,
                Status = status
            },
            TenantId = tenantId
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get user transactions
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? transactionTypeId = null,
        [FromQuery] TransactionStatus? status = null)
    {
        var tenantId = GetTenantId();
        var query = new GetTransactionsQuery
        {
            Filter = new TransactionFilterDto
            {
                UserId = userId,
                FromDate = startDate,
                ToDate = endDate,
                TransactionTypeId = transactionTypeId,
                Status = status
            },
            TenantId = tenantId
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private Guid GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenant_id") ?? User.FindFirst(ClaimTypes.GroupSid);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        throw new UnauthorizedAccessException("Tenant ID not found in claims");
    }
}