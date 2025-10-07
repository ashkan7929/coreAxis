using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.Modules.WalletModule.Infrastructure.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    /// Export reconciliation report per-account and per-currency in CSV/JSON
    /// </summary>
    [HttpGet("reports/reconciliation/export")] // GET api/admin/wallet/reports/reconciliation/export?from=&to=&format=csv|json
    [RequirePermission("WALLET.ADMIN", "Read")]
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

    [HttpGet("reconcile")] // GET admin/wallet/reconcile
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

    [HttpGet("snapshot/{walletId:guid}")] // GET admin/wallet/snapshot/{walletId}
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
    /// Lock a wallet by admin with a reason
    /// </summary>
    [HttpPost("{id:guid}/lock")] // POST api/admin/wallet/{id}/lock
    [RequirePermission("WALLET.ADMIN", "Write")]
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
    /// Unlock a wallet by admin
    /// </summary>
    [HttpPost("{id:guid}/unlock")] // POST api/admin/wallet/{id}/unlock
    [RequirePermission("WALLET.ADMIN", "Write")]
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