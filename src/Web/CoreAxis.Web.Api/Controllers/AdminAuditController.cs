using CoreAxis.SharedKernel.Observability.Audit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace CoreAxis.Web.Api.Controllers;

[ApiController]
[Route("api/admin/audit")]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditStore _auditStore;
    private readonly ILogger<AdminAuditController> _logger;

    public AdminAuditController(IAuditStore auditStore, ILogger<AdminAuditController> logger)
    {
        _auditStore = auditStore;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? userId,
        [FromQuery] string? orderId,
        [FromQuery] string? txId,
        [FromQuery] string? type,
        [FromQuery] string? severity,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        var items = await _auditStore.QueryAsync(
            correlationId: null,
            userId: userId,
            orderId: orderId,
            txId: txId,
            eventType: type,
            severity: severity,
            page: page,
            size: size);

        return Ok(new { items, total = items.Count, page, size });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? format = "json",
        [FromQuery] string? userId = null,
        [FromQuery] string? orderId = null,
        [FromQuery] string? txId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 1000)
    {
        var items = await _auditStore.QueryAsync(
            correlationId: null,
            userId: userId,
            orderId: orderId,
            txId: txId,
            eventType: type,
            severity: severity,
            page: page,
            size: size);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,Module,EventType,CorrelationId,UserId,OrderId,TxId,Severity");
            foreach (var i in items)
            {
                csv.AppendLine(string.Join(',', new[]
                {
                    i.Id.ToString(),
                    i.Timestamp.ToString("o", CultureInfo.InvariantCulture),
                    Escape(i.Module),
                    Escape(i.EventType),
                    Escape(i.CorrelationId),
                    Escape(i.UserId),
                    Escape(i.OrderId),
                    Escape(i.TxId),
                    Escape(i.Severity)
                }));
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "audit-export.csv");
        }

        return Ok(items);

        static string Escape(string? s)
            => string.IsNullOrEmpty(s) ? "" : s.Replace(",", ";");
    }
}