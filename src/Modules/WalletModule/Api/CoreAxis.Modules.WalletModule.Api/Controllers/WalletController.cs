using CoreAxis.Modules.WalletModule.Application.Commands;
using CoreAxis.Modules.WalletModule.Application.DTOs;
using CoreAxis.Modules.WalletModule.Application.Queries;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using CoreAxis.Modules.WalletModule.Api.Filters;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using CoreAxis.Modules.WalletModule.Api.Observability;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CoreAxis.Modules.WalletModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WalletController> _logger;
    private readonly WalletDbContext _context;

    public WalletController(IMediator mediator, ILogger<WalletController> logger, WalletDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _context = context;
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
    // [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<WalletDto>> GetWallet(Guid id)
    {
        try
        {
            // Log authentication info for debugging
            _logger.LogInformation("GetWallet called with ID: {WalletId}", id);
            _logger.LogInformation("User authenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
            _logger.LogInformation("User claims count: {ClaimsCount}", User.Claims.Count());
            
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserId();
                _logger.LogInformation("User ID from claims: {UserId}", userId);
            }
            
            var query = new GetWalletByIdQuery { WalletId = id };
            var result = await _mediator.Send(query);
            
            if (result == null)
                return NotFound();
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetWallet for ID: {WalletId}", id);
            throw;
        }
    }

    /// <summary>
    /// Get wallets for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    // [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<IEnumerable<WalletDto>>> GetUserWallets(Guid userId)
    {
        var query = new GetUserWalletsQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Wallet statements with cursor-based pagination and CSV/JSON export
    /// </summary>
    [HttpGet("statements")] // GET /api/wallet/statements?walletId=&from=&to=&format=csv|json&cursor=&limit=
    public async Task<IActionResult> GetStatements([FromQuery] Guid walletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? format = "json", [FromQuery] string? cursor = null, [FromQuery] int? limit = 50, CancellationToken cancellationToken = default)
    {
        if (walletId == Guid.Empty)
        {
            return BadRequest(new { message = "walletId is required" });
        }

        var start = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = to ?? DateTime.UtcNow;
        var take = Math.Clamp(limit ?? 50, 1, 500);

        DateTime? afterCreatedOn = null;
        Guid? afterId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var parts = cursor.Split(':');
            if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParse(parts[1], out var gid))
            {
                afterCreatedOn = new DateTime(ticks, DateTimeKind.Utc);
                afterId = gid;
            }
        }

        var query = _context.Transactions
            .Include(t => t.TransactionType)
            .Where(t => t.WalletId == walletId && t.CreatedOn >= start && t.CreatedOn <= end);

        if (afterCreatedOn.HasValue && afterId.HasValue)
        {
            query = query.Where(t => t.CreatedOn > afterCreatedOn.Value || (t.CreatedOn == afterCreatedOn.Value && t.Id.CompareTo(afterId.Value) > 0));
        }

        var items = await query
            .OrderBy(t => t.CreatedOn).ThenBy(t => t.Id)
            .Take(take)
            .Select(t => new
            {
                date = t.CreatedOn,
                reference = t.Reference,
                type = t.TransactionType.Code,
                amount = t.Amount,
                balanceAfter = t.BalanceAfter,
                id = t.Id
            })
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (items.Count > 0)
        {
            var last = items[^1];
            nextCursor = $"{last.date.Ticks}:{last.id}";
        }

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("date,ref,type,amount,balanceAfter");
            foreach (var it in items)
            {
                // Use ISO8601 UTC for date
                var dateStr = it.date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                sb.AppendLine($"{dateStr},{it.reference},{it.type},{it.amount},{it.balanceAfter}");
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"wallet_statements_{walletId}_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
        }

        var result = new
        {
            walletId,
            from = start,
            to = end,
            items = items.Select(it => new Dictionary<string, object?>
            {
                ["date"] = it.date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["ref"] = it.reference,
                ["type"] = it.type,
                ["amount"] = it.amount,
                ["balanceAfter"] = it.balanceAfter
            }),
            nextCursor
        };

        return Ok(result);
    }

    /// <summary>
    /// Get wallet balance
    /// </summary>
    [HttpGet("{id}/balance")]
    // [HasPermission("WALLET", "READ")]
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
    [RateLimit(30, 60)]
    // [HasPermission("WALLET", "DEPOSIT")]
    public async Task<ActionResult<TransactionResultDto>> Deposit(Guid id, [FromBody] DepositRequestDto request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            WalletMetrics.Failures.Add(1, new TagList { { "endpoint", "deposit" }, { "code", "WLT_IDEMPOTENCY_REQUIRED" } });
            return ProblemWithExtensions(
                title: "Idempotency key required",
                detail: "Provide 'Idempotency-Key' header to ensure safe retries.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://coreaxis.dev/problems/wallet/wlt_idempotency_required",
                code: "WLT_IDEMPOTENCY_REQUIRED"
            );
        }
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var sw = Stopwatch.StartNew();
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
        sw.Stop();
        if (!result.Success)
        {
            var code = result.Code ?? "WLT_POLICY_VIOLATION";
            WalletMetrics.Failures.Add(1, new TagList { { "endpoint", "deposit" }, { "code", code } });
            WalletMetrics.DepositLatencyMs.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "endpoint", "deposit" } });
            return ToProblem(result);
        }
        WalletMetrics.Deposits.Add(1, new TagList { { "endpoint", "deposit" } });
        WalletMetrics.DepositLatencyMs.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "endpoint", "deposit" } });
        return Ok(result);
    }

    /// <summary>
    /// Withdraw money from wallet
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [RateLimit(30, 60)]
    // [HasPermission("WALLET", "WITHDRAW")]
    public async Task<ActionResult<TransactionResultDto>> Withdraw(Guid id, [FromBody] WithdrawRequestDto request)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            WalletMetrics.Failures.Add(1, new TagList { { "endpoint", "withdraw" }, { "code", "WLT_IDEMPOTENCY_REQUIRED" } });
            return ProblemWithExtensions(
                title: "Idempotency key required",
                detail: "Provide 'Idempotency-Key' header to ensure safe retries.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://coreaxis.dev/problems/wallet/wlt_idempotency_required",
                code: "WLT_IDEMPOTENCY_REQUIRED"
            );
        }
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var sw = Stopwatch.StartNew();
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
        sw.Stop();
        if (!result.Success)
        {
            var code = result.Code ?? "WLT_POLICY_VIOLATION";
            WalletMetrics.Failures.Add(1, new TagList { { "endpoint", "withdraw" }, { "code", code } });
            WalletMetrics.WithdrawLatencyMs.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "endpoint", "withdraw" } });
            return ToProblem(result);
        }
        WalletMetrics.Withdrawals.Add(1, new TagList { { "endpoint", "withdraw" } });
        WalletMetrics.WithdrawLatencyMs.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "endpoint", "withdraw" } });
        return Ok(result);
    }

    /// <summary>
    /// Transfer money between wallets
    /// </summary>
    [HttpPost("{id}/transfer")]
    [RateLimit(30, 60)]
    // [HasPermission("WALLET", "TRANSFER")]
    public async Task<ActionResult<TransactionResultDto>> Transfer(Guid id, [FromBody] TransferRequestDto request)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            WalletMetrics.Failures.Add(1, new TagList { { "endpoint", "transfer" }, { "code", "WLT_IDEMPOTENCY_REQUIRED" } });
            return ProblemWithExtensions(
                title: "Idempotency key required",
                detail: "Provide 'Idempotency-Key' header to ensure safe retries.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://coreaxis.dev/problems/wallet/wlt_idempotency_required",
                code: "WLT_IDEMPOTENCY_REQUIRED"
            );
        }
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var sw = Stopwatch.StartNew();
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
        sw.Stop();
        if (!result.Success)
        {
            var code = result.Code ?? "WLT_POLICY_VIOLATION";
            WalletMetrics.Failures.Add(1, new TagList { { "endpoint", "transfer" }, { "code", code } });
            WalletMetrics.TransferLatencyMs.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "endpoint", "transfer" } });
            return ToProblem(result);
        }
        WalletMetrics.Transfers.Add(1, new TagList { { "endpoint", "transfer" } });
        WalletMetrics.TransferLatencyMs.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "endpoint", "transfer" } });
        return Ok(result);
    }

    /// <summary>
    /// Get wallet transactions
    /// </summary>
    [HttpGet("{id}/transactions")]
    // [HasPermission("WALLET", "READ")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
        Guid id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? transactionTypeId = null,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int? limit = null)
    {
        var query = new GetTransactionsQuery
        {
            Filter = new TransactionFilterDto
            {
                WalletId = id,
                FromDate = startDate,
                ToDate = endDate,
                TransactionTypeId = transactionTypeId,
                Status = status,
                Cursor = cursor,
                Limit = limit
            }
        };

        var result = (await _mediator.Send(query)).ToList();

        // Compute and expose next cursor header for clients (non-breaking change)
        var pageLimit = limit ?? 50;
        if (result.Count == pageLimit && result.Count > 0)
        {
            var last = result[^1];
            var raw = $"{last.CreatedOn.Ticks}:{last.Id}";
            var nextCursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
            Response.Headers["X-Next-Cursor"] = nextCursor;
        }

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

    private ActionResult ToProblem(TransactionResultDto result)
    {
        var code = result.Code ?? "WLT_POLICY_VIOLATION";
        var status = code switch
        {
            "WLT_RATE_LIMIT" => StatusCodes.Status429TooManyRequests,
            "WLT_ACCOUNT_FROZEN" => StatusCodes.Status423Locked,
            _ => StatusCodes.Status400BadRequest
        };
        var type = code switch
        {
            "WLT_NEGATIVE_BLOCKED" => "https://coreaxis.dev/problems/wallet/wlt_negative_blocked",
            "WLT_ACCOUNT_FROZEN" => "https://coreaxis.dev/problems/wallet/wlt_account_frozen",
            "WLT_INVALID_TRANSFER" => "https://coreaxis.dev/problems/wallet/wlt_invalid_transfer",
            "WLT_CONCURRENCY_CONFLICT" => "https://coreaxis.dev/problems/wallet/wlt_concurrency_conflict",
            "WLT_RATE_LIMIT" => "https://coreaxis.dev/problems/wallet/wlt_rate_limit",
            _ => "https://coreaxis.dev/problems/wallet/wlt_policy_violation"
        };

        return ProblemWithExtensions(
            title: result.Message,
            detail: result.Errors.FirstOrDefault() ?? result.Message,
            statusCode: status,
            type: type,
            code: code
        );
    }

    private ObjectResult ProblemWithExtensions(string title, string detail, int statusCode, string type, string code)
    {
        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = type
        };
        problem.Extensions["code"] = code;
        return StatusCode(statusCode, problem);
    }
}