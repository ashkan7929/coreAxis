using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.Modules.WalletModule.Infrastructure.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using CoreAxis.SharedKernel.Authorization;

namespace CoreAxis.Modules.WalletModule.Api.Controllers;

[ApiController]
[Route("admin/wallet")] // Admin endpoints for wallet operations (legacy)
[Route("api/admin/wallet")] // Preferred admin API route
[Authorize]
public class AdminWalletController : ControllerBase
{
    private readonly WalletDbContext _context;
    private readonly IBalanceSnapshotProvider _snapshotProvider;
    private readonly ILogger<AdminWalletController> _logger;

    public AdminWalletController(WalletDbContext context, IBalanceSnapshotProvider snapshotProvider, ILogger<AdminWalletController> logger)
    {
        _context = context;
        _snapshotProvider = snapshotProvider;
        _logger = logger;
    }

    /// <summary>
    /// Export reconciliation report per-account and per-currency.
    /// </summary>
    /// <remarks>
    /// Returns either JSON or CSV based on the `format` query parameter.
    ///
    /// Example (JSON):
    ///
    /// ```json
    /// {
    ///   "from": "2025-01-01T00:00:00Z",
    ///   "to": "2025-01-08T00:00:00Z",
    ///   "accounts": [
    ///     {
    ///       "walletId": "a1b2c3d4-0000-0000-0000-000000000001",
    ///       "currency": "USD",
    ///       "totalCredits": 1200.00,
    ///       "totalDebits": 450.00,
    ///       "netChange": 750.00,
    ///       "startBalance": 100.00,
    ///       "endBalance": 850.00,
    ///       "mismatch": 0.00
    ///     }
    ///   ],
    ///   "currencies": [
    ///     {
    ///       "currency": "USD",
    ///       "totalCredits": 1200.00,
    ///       "totalDebits": 450.00,
    ///       "netChange": 750.00,
    ///       "mismatch": 0.00
    ///     }
    ///   ]
    /// }
    /// ```
    ///
    /// Example (CSV):
    ///
    /// ```csv
    /// section,walletId,currency,totalCredits,totalDebits,netChange,startBalance,endBalance,mismatch
    /// account,a1b2c3...,USD,1200,450,750,100,850,0
    ///
    /// section,currency,totalCredits,totalDebits,netChange,mismatch
    /// currency,USD,1200,450,750,0
    /// ```
    /// </remarks>
    /// <param name="from">Start date (UTC). Defaults to last 7 days.</param>
    /// <param name="to">End date (UTC). Defaults to now.</param>
    /// <param name="format">Output format: `json` (default) or `csv`.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    [HttpGet("reports/reconciliation/export")] // GET api/admin/wallet/reports/reconciliation/export?from=&to=&format=csv|json
    [RequirePermission("WALLET.ADMIN", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportReconciliationAsync([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? format = "json", CancellationToken cancellationToken = default)
    {
        var start = from ?? DateTime.UtcNow.Date.AddDays(-7);
        var end = to ?? DateTime.UtcNow;

        var creditCodes = new[] { "DEPOSIT", "TRANSFER_IN", "COMMISSION", "ADJUSTMENT_CREDIT" };
        var debitCodes = new[] { "WITHDRAW", "TRANSFER_OUT", "ADJUSTMENT_DEBIT" };

        // Load transactions in range with required navigation properties
        var txs = await _context.Transactions
            .Include(t => t.Wallet)
            .Include(t => t.TransactionType)
            .Where(t => t.CreatedOn >= start && t.CreatedOn <= end)
            .OrderBy(t => t.CreatedOn).ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var perAccount = txs
            .GroupBy(t => new { t.WalletId, t.Wallet.Currency })
            .Select(g =>
            {
                var credits = g.Where(t => creditCodes.Contains(t.TransactionType.Code)).Sum(t => t.Amount);
                var debits = g.Where(t => debitCodes.Contains(t.TransactionType.Code)).Sum(t => t.Amount);
                var net = credits - debits;
                var firstTx = g.FirstOrDefault();
                var lastTx = g.LastOrDefault();
                decimal startBalance;
                if (firstTx != null)
                {
                    var firstSigned = creditCodes.Contains(firstTx.TransactionType.Code) ? firstTx.Amount : -firstTx.Amount;
                    startBalance = firstTx.BalanceAfter - firstSigned;
                }
                else
                {
                    // No transactions in range; approximate with current balance
                    var wallet = _context.Wallets.FirstOrDefault(w => w.Id == g.Key.WalletId);
                    startBalance = wallet?.Balance ?? 0m;
                }
                var endBalance = lastTx?.BalanceAfter ?? startBalance;
                var mismatch = (endBalance - startBalance) - net;

                return new
                {
                    walletId = g.Key.WalletId,
                    currency = g.Key.Currency,
                    totalCredits = credits,
                    totalDebits = debits,
                    netChange = net,
                    startBalance,
                    endBalance,
                    mismatch
                };
            })
            .ToList();

        var perCurrency = perAccount
            .GroupBy(a => a.currency)
            .Select(g => new
            {
                currency = g.Key,
                totalCredits = g.Sum(x => x.totalCredits),
                totalDebits = g.Sum(x => x.totalDebits),
                netChange = g.Sum(x => x.netChange),
                mismatch = g.Sum(x => x.mismatch)
            })
            .OrderBy(x => x.currency)
            .ToList();

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            // Per-account section
            sb.AppendLine("section,walletId,currency,totalCredits,totalDebits,netChange,startBalance,endBalance,mismatch");
            foreach (var a in perAccount)
            {
                sb.AppendLine($"account,{a.walletId},{a.currency},{a.totalCredits},{a.totalDebits},{a.netChange},{a.startBalance},{a.endBalance},{a.mismatch}");
            }
            sb.AppendLine();
            // Per-currency section
            sb.AppendLine("section,currency,totalCredits,totalDebits,netChange,mismatch");
            foreach (var c in perCurrency)
            {
                sb.AppendLine($"currency,{c.currency},{c.totalCredits},{c.totalDebits},{c.netChange},{c.mismatch}");
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"wallet_reconciliation_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
        }

        var json = new
        {
            from = start,
            to = end,
            accounts = perAccount,
            currencies = perCurrency
        };
        return Ok(json);
    }

    /// <summary>
    /// Get high-level reconciliation status and counters.
    /// </summary>
    /// <remarks>
    /// Provides pending commission transactions count, locked wallets count,
    /// total active wallets, and snapshot cache size.
    ///
    /// Example response:
    ///
    /// ```json
    /// {
    ///   "PendingCommissionTransactions": 12,
    ///   "LockedWallets": 3,
    ///   "ActiveWallets": 1245,
    ///   "SnapshotCacheCount": 58
    /// }
    /// ```
    /// </remarks>
    [HttpGet("reconcile")] // GET admin/wallet/reconcile
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReconciliationAsync(CancellationToken cancellationToken)
    {
        // Commission type id by code
        var commissionTypeId = await _context.TransactionTypes
            .Where(t => t.Code == "COMMISSION")
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingCommissions = commissionTypeId == Guid.Empty
            ? 0
            : await _context.Transactions
                .Where(t => t.TransactionTypeId == commissionTypeId && t.Status == Domain.Entities.TransactionStatus.Pending)
                .CountAsync(cancellationToken);

        var lockedWallets = await _context.Wallets
            .Where(w => w.IsLocked)
            .CountAsync(cancellationToken);

        var activeWallets = await _context.Wallets.CountAsync(cancellationToken);

        var result = new
        {
            PendingCommissionTransactions = pendingCommissions,
            LockedWallets = lockedWallets,
            ActiveWallets = activeWallets,
            SnapshotCacheCount = _snapshotProvider.SnapshotCount
        };

        return Ok(result);
    }

    /// <summary>
    /// Get cached balance snapshot for a wallet.
    /// </summary>
    /// <remarks>
    /// Returns a snapshot if available from the cache provider; otherwise,
    /// falls back to current wallet balance and stores a new snapshot.
    /// </remarks>
    /// <param name="walletId">Wallet unique identifier.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    [HttpGet("snapshot/{walletId:guid}")] // GET admin/wallet/snapshot/{walletId}
    [ProducesResponseType(typeof(BalanceSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSnapshotAsync([FromRoute] Guid walletId, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotProvider.GetSnapshotAsync(walletId, cancellationToken);
        if (snapshot != null)
        {
            return Ok(snapshot);
        }

        var wallet = await _context.Wallets
            .Where(w => w.Id == walletId)
            .Select(w => new { w.Id, w.Balance })
            .FirstOrDefaultAsync(cancellationToken);

        if (wallet == null)
        {
            return NotFound(new { message = "Wallet not found" });
        }

        // Return a snapshot-like response on the fly
        var fallback = new BalanceSnapshot
        {
            WalletId = wallet.Id,
            Balance = wallet.Balance,
            CapturedAt = DateTime.UtcNow
        };

        // Optionally store for future calls
        await _snapshotProvider.SetSnapshotAsync(wallet.Id, wallet.Balance, fallback.CapturedAt, cancellationToken);

        return Ok(fallback);
    }

    /// <summary>
    /// Lock a wallet by admin with a reason.
    /// </summary>
    /// <remarks>
    /// Example request body:
    ///
    /// ```json
    /// {
    ///   "reason": "Suspicious activity detected"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("{id:guid}/lock")] // POST api/admin/wallet/{id}/lock
    [RequirePermission("WALLET.ADMIN", "Write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LockWalletAsync([FromRoute] Guid id, [FromBody] LockRequest request, CancellationToken cancellationToken)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (wallet == null)
        {
            return NotFound(new { message = "Wallet not found" });
        }

        if (wallet.IsLocked)
        {
            return Conflict(new { message = "Wallet is already locked", reason = wallet.LockReason });
        }

        wallet.Lock(request?.Reason ?? "");
        await _context.SaveChangesAsync(cancellationToken);

        // DoD: return 204 NoContent on successful lock
        return NoContent();
    }

    /// <summary>
    /// Unlock a wallet by admin.
    /// </summary>
    [HttpPost("{id:guid}/unlock")] // POST api/admin/wallet/{id}/unlock
    [RequirePermission("WALLET.ADMIN", "Write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnlockWalletAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (wallet == null)
        {
            return NotFound(new { message = "Wallet not found" });
        }

        if (!wallet.IsLocked)
        {
            return Conflict(new { message = "Wallet is not locked" });
        }

        wallet.Unlock();
        await _context.SaveChangesAsync(cancellationToken);

        // DoD: return 204 NoContent on successful unlock
        return NoContent();
    }

    public record LockRequest(string Reason);
}