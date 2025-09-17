using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Payments;

public record GetPaymentByIdQuery(Guid Id) : IRequest<PaymentDto?>;

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentByIdQueryHandler> _logger;

    public GetPaymentByIdQueryHandler(
        IPaymentRepository paymentRepository,
        ILogger<GetPaymentByIdQueryHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdWithRefundsAsync(request.Id);
            if (payment == null)
            {
                _logger.LogWarning("Payment with ID {PaymentId} not found", request.Id);
                return null;
            }

            var paymentDto = new PaymentDto
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
            };

            _logger.LogInformation("Retrieved payment with ID: {PaymentId}", request.Id);
            return paymentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment with ID: {PaymentId}", request.Id);
            throw;
        }
    }
}