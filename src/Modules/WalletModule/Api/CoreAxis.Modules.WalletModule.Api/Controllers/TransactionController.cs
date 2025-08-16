using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Queries;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.API.Authz;
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
    [HasPermission("WALLET", "READ")]
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
    [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromQuery] Guid? walletId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? transactionTypeId = null,
        [FromQuery] TransactionStatus? status = null)
    {
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
            }
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get user transactions
    /// </summary>
    [HttpGet("user/{userId}")]
    [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? transactionTypeId = null,
        [FromQuery] TransactionStatus? status = null)
    {
        var query = new GetTransactionsQuery
        {
            Filter = new TransactionFilterDto
            {
                UserId = userId,
                FromDate = startDate,
                ToDate = endDate,
                TransactionTypeId = transactionTypeId,
                Status = status
            }
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }


}