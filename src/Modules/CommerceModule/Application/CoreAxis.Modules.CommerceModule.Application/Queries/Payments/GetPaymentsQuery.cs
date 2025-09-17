using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Payments;

public record GetPaymentsQuery(
    Guid? OrderId = null,
    string? Status = null,
    string? PaymentMethod = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<(List<PaymentDto> Payments, int TotalCount)>;

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, (List<PaymentDto> Payments, int TotalCount)>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentsQueryHandler> _logger;

    public GetPaymentsQueryHandler(
        IPaymentRepository paymentRepository,
        ILogger<GetPaymentsQueryHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<(List<PaymentDto> Payments, int TotalCount)> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (payments, totalCount) = await _paymentRepository.GetPaymentsAsync(
                request.OrderId,
                request.Status,
                request.PaymentMethod,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize);

            var paymentDtos = payments.Select(payment => new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                GatewayResponse = payment.GatewayResponse,
                ProcessedAt = payment.ProcessedAt,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                Refunds = payment.Refunds?.Select(refund => new RefundDto
                {
                    Id = refund.Id,
                    PaymentId = refund.PaymentId,
                    Amount = refund.Amount,
                    Currency = refund.Currency,
                    Reason = refund.Reason,
                    Status = refund.Status,
                    RefundTransactionId = refund.RefundTransactionId,
                    ProcessedAt = refund.ProcessedAt,
                    FailureReason = refund.FailureReason,
                    CreatedAt = refund.CreatedAt,
                    UpdatedAt = refund.UpdatedAt
                }).ToList() ?? new List<RefundDto>()
            }).ToList();

            _logger.LogInformation("Retrieved {Count} payments out of {TotalCount} total payments", 
                paymentDtos.Count, totalCount);

            return (paymentDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            throw;
        }
    }
}