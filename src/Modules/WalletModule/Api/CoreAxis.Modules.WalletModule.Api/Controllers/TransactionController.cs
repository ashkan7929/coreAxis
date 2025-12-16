using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Queries;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
    /// Get a transaction by its unique identifier.
    /// </summary>
    /// <remarks>
    /// Retrieves full transaction details including amount, type, status, and timestamps.
    ///
    /// Sample request:
    ///
    /// GET /api/transaction/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///
    /// Sample 200 response body (JSON):
    ///
    /// {
    ///   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "walletId": "a1b2c3d4-0000-0000-0000-000000000001",
    ///   "amount": 250.00,
    ///   "status": "Completed",
    ///   "type": {
    ///     "id": "0d9e6bca-0000-0000-0000-000000000123",
    ///     "code": "DEPOSIT",
    ///     "name": "Deposit"
    ///   },
    ///   "createdOn": "2025-01-08T12:34:56Z",
    ///   "reference": "ORD-100045",
    ///   "description": "Initial funding"
    /// }
    ///
    /// Status codes:
    /// - 200 OK: Transaction found
    /// - 401 Unauthorized: Authentication required
    /// - 404 Not Found: Transaction does not exist
    /// </remarks>
    [HttpGet("{id}")]
    [HasPermission("WALLET", "READ")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetTransaction([FromRoute] Guid id)
    {
        var query = new GetTransactionByIdQuery { TransactionId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get transactions with filters.
    /// </summary>
    /// <remarks>
    /// Filter by wallet, user, date range, type, and status. Returns a list of transactions.
    ///
    /// Example:
    /// GET /api/transaction?walletId=a1b2c3d4-...&startDate=2025-01-01&endDate=2025-01-08&status=Completed
    ///
    /// Status codes:
    /// - 200 OK: Successful retrieval
    /// - 401 Unauthorized: Authentication required
    /// </remarks>
    [HttpGet]
    // [HasPermission("WALLET", "READ")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Get transactions for a specific user.
    /// </summary>
    /// <remarks>
    /// Retrieves all transactions associated with the given `userId`.
    ///
    /// Example:
    /// GET /api/transaction/user/3fa85f64-5717-4562-b3fc-2c963f66afa6?startDate=2025-01-01&endDate=2025-01-08
    ///
    /// Status codes:
    /// - 200 OK: Successful retrieval
    /// - 401 Unauthorized: Authentication required
    /// </remarks>
    [HttpGet("user/{userId}")]
    [HasPermission("WALLET", "READ")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        [FromRoute] Guid userId,
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