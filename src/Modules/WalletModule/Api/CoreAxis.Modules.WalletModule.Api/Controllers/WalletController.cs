using CoreAxis.Modules.WalletModule.Application.Commands;
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
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new wallet
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WalletDto>> CreateWallet([FromBody] CreateWalletDto request)
    {
        var command = new CreateWalletCommand
        {
            UserId = request.UserId,
            WalletTypeId = request.WalletTypeId,
            Currency = request.Currency
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWallet), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get wallet by ID
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<WalletDto>> GetWallet(Guid id)
    {
        var query = new GetWalletByIdQuery { WalletId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get wallets for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<IEnumerable<WalletDto>>> GetUserWallets(Guid userId)
    {
        var query = new GetUserWalletsQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get wallet balance
    /// </summary>
    [HttpGet("{id}/balance")]
    [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<WalletBalanceDto>> GetWalletBalance(Guid id)
    {
        var query = new GetWalletBalanceQuery { WalletId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Deposit money to wallet
    /// </summary>
    [HttpPost("{id}/deposit")]
    [HasPermission("WALLET", "DEPOSIT")]
    public async Task<ActionResult<TransactionResultDto>> Deposit(Guid id, [FromBody] DepositRequestDto request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var command = new DepositCommand
        {
            WalletId = id,
            Amount = request.Amount,
            Description = request.Description,
            Reference = request.Reference,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Withdraw money from wallet
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [HasPermission("WALLET", "WITHDRAW")]
    public async Task<ActionResult<TransactionResultDto>> Withdraw(Guid id, [FromBody] WithdrawRequestDto request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var command = new WithdrawCommand
        {
            WalletId = id,
            Amount = request.Amount,
            Description = request.Description,
            Reference = request.Reference,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Transfer money between wallets
    /// </summary>
    [HttpPost("{id}/transfer")]
    [HasPermission("WALLET", "TRANSFER")]
    public async Task<ActionResult<TransactionResultDto>> Transfer(Guid id, [FromBody] TransferRequestDto request)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var command = new TransferCommand
        {
            FromWalletId = id,
            ToWalletId = request.ToWalletId,
            Amount = request.Amount,
            Description = request.Description,
            Reference = request.Reference,
            Metadata = request.Metadata,
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Get wallet transactions
    /// </summary>
    [HttpGet("{id}/transactions")]
    [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        Guid id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? transactionTypeId = null,
        [FromQuery] TransactionStatus? status = null)
    {
        var query = new GetTransactionsQuery
        {
            Filter = new TransactionFilterDto
            {
                WalletId = id,
                FromDate = startDate,
                ToDate = endDate,
                TransactionTypeId = transactionTypeId,
                Status = status
            }
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in claims");
    }
}