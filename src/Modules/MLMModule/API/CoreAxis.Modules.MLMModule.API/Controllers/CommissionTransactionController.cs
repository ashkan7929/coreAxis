using CoreAxis.Modules.MLMModule.Application.Commands;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreAxis.SharedKernel.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CoreAxis.Modules.MLMModule.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommissionTransactionController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionTransactionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get commission by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(CommissionTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionTransactionDto>> GetCommission(Guid id)
    {
        var query = new GetCommissionByIdQuery { CommissionId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get user commissions
    /// </summary>
    [HttpGet("user/{userId}")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionTransactionDto>>> GetUserCommissions(
        Guid userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = new GetUserCommissionsQuery 
        { 
            UserId = userId, 
            Filter = new CommissionFilterDto { PageNumber = page, PageSize = pageSize }
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get commissions by status
    /// </summary>
    [HttpGet("status/{status}")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionTransactionDto>>> GetCommissionsByStatus(
        CommissionStatus status, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = new GetCommissionsByStatusQuery 
        { 
            Status = status, 
            PageNumber = page, 
            PageSize = pageSize 
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get commissions by source payment
    /// </summary>
    [HttpGet("source-payment/{sourcePaymentId}")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionTransactionDto>>> GetCommissionsBySourcePayment(Guid sourcePaymentId)
    {
        var query = new GetCommissionsBySourcePaymentQuery { SourcePaymentId = sourcePaymentId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get commission summary for user
    /// </summary>
    [HttpGet("user/{userId}/summary")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(CommissionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommissionSummaryDto>> GetCommissionSummary(
        Guid userId, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetCommissionSummaryQuery 
        { 
            UserId = userId, 
            FromDate = startDate, 
            ToDate = endDate 
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get pending commissions for approval
    /// </summary>
    [HttpGet("pending-approval")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionTransactionDto>>> GetPendingCommissionsForApproval(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPendingCommissionsForApprovalQuery 
        { 
            PageNumber = page, 
            PageSize = pageSize 
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get commissions by date range
    /// </summary>
    [HttpGet("date-range")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommissionTransactionDto>>> GetCommissionsByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = new GetCommissionsByDateRangeQuery 
        { 
            FromDate = startDate, 
            ToDate = endDate
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Approve commission
    /// </summary>
    [HttpPost("{id}/approve")]
    [RequirePermission("Commissions", "Approve")]
    [EnableRateLimiting("mlm-actions")]
    [ProducesResponseType(typeof(CommissionTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CommissionTransactionDto>> ApproveCommission(Guid id, [FromBody] ApproveCommissionDto request)
    {
        var command = new ApproveCommissionCommand
        {
            CommissionId = id,
            ApprovedBy = GetCurrentUserId(),
            Notes = request.ApprovalNotes
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Reject commission
    /// </summary>
    [HttpPost("{id}/reject")]
    [RequirePermission("Commissions", "Reject")]
    [EnableRateLimiting("mlm-actions")]
    [ProducesResponseType(typeof(CommissionTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CommissionTransactionDto>> RejectCommission(Guid id, [FromBody] RejectCommissionDto request)
    {
        var command = new RejectCommissionCommand
        {
            CommissionId = id,
            RejectedBy = GetCurrentUserId(),
            RejectionReason = request.RejectionReason
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Mark commission as paid
    /// </summary>
    [HttpPost("{id}/mark-paid")]
    [RequirePermission("Commissions", "MarkPaid")]
    [EnableRateLimiting("mlm-actions")]
    [ProducesResponseType(typeof(CommissionTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CommissionTransactionDto>> MarkCommissionAsPaid(Guid id, [FromBody] MarkCommissionAsPaidDto request)
    {
        var command = new MarkCommissionAsPaidCommand
        {
            CommissionId = id,
            PaidBy = GetCurrentUserId(),
            WalletTransactionId = Guid.NewGuid(), // This should be provided from the request
            Notes = $"Payment Reference: {request.PaymentReference}, Method: {request.PaymentMethod}"
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Admin commissions report with optional filters and CSV/JSON output
    /// </summary>
    [HttpGet("admin/report")]
    [RequirePermission("Commissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CommissionTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminReport(
        [FromQuery] Guid? userId,
        [FromQuery] CommissionStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string format = "json")
    {
        IEnumerable<CommissionTransactionDto> result;

        // Prefer most specific query based on provided filters
        if (userId.HasValue)
        {
            var query = new GetUserCommissionsQuery
            {
                UserId = userId.Value,
                Filter = new CommissionFilterDto
                {
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    PageNumber = page,
                    PageSize = pageSize
                }
            };
            result = await _mediator.Send(query);
        }
        else if (status.HasValue)
        {
            var query = new GetCommissionsByStatusQuery
            {
                Status = status.Value,
                PageNumber = page,
                PageSize = pageSize
            };
            result = await _mediator.Send(query);
        }
        else if (fromDate.HasValue && toDate.HasValue)
        {
            var query = new GetCommissionsByDateRangeQuery
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value
            };
            result = await _mediator.Send(query);
        }
        else
        {
            // Fallback to all commissions paged
            var query = new GetAllCommissionsQuery
            {
                PageNumber = page,
                PageSize = pageSize
            };
            result = await _mediator.Send(query);
        }

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,UserId,SourcePaymentId,ProductId,RuleSetId,Level,Amount,SourceAmount,Percentage,Status,IsSettled,WalletTransactionId,CreatedOn,ApprovedAt,PaidAt,RejectedAt");
            foreach (var c in result)
            {
                sb.AppendLine($"{c.Id},{c.UserId},{c.SourcePaymentId},{c.ProductId},{c.CommissionRuleSetId},{c.Level},{c.Amount},{c.SourceAmount},{c.Percentage},{c.Status},{c.IsSettled},{c.WalletTransactionId},{c.CreatedOn:o},{c.ApprovedAt:o},{c.PaidAt:o},{c.RejectedAt:o}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"commissions_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        return Ok(result);
    }

    /// <summary>
    /// Process pending commissions
    /// </summary>
    [HttpPost("process-pending")]
    [RequirePermission("Commissions", "Process")]
    public async Task<ActionResult<ProcessPendingCommissionsResultDto>> ProcessPendingCommissions([FromBody] ProcessPendingCommissionsDto request)
    {
        var command = new ProcessPendingCommissionsCommand
        {
            ProcessedBy = GetCurrentUserId(),
            BatchSize = request.BatchSize,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);
        return Ok(new ProcessPendingCommissionsResultDto
        {
            ProcessedCount = result.Count,
            ProcessedCommissions = result
        });
    }

    /// <summary>
    /// Update commission notes
    /// </summary>
    [HttpPut("{id}/notes")]
    [RequirePermission("Commissions", "Update")]
    public async Task<ActionResult<CommissionTransactionDto>> UpdateCommissionNotes(Guid id, [FromBody] UpdateCommissionNotesDto request)
    {
        var command = new UpdateCommissionNotesCommand
        {
            CommissionId = id,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Expire commission
    /// </summary>
    [HttpPost("{id}/expire")]
    [RequirePermission("Commissions", "Expire")]
    public async Task<ActionResult<CommissionTransactionDto>> ExpireCommission(Guid id, [FromBody] ExpireCommissionDto request)
    {
        var command = new ExpireCommissionCommand
        {
            CommissionId = id
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}