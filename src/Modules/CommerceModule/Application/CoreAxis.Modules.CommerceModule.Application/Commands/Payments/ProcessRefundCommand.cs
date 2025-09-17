using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Payments;

public record ProcessRefundCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason
) : IRequest<RefundDto>;

public class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, RefundDto>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly ILogger<ProcessRefundCommandHandler> _logger;

    public ProcessRefundCommandHandler(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        ILogger<ProcessRefundCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _logger = logger;
    }

    public async Task<RefundDto> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify payment exists and is processed
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment with ID {request.PaymentId} not found.");
            }

            if (payment.Status != "Completed")
            {
                throw new InvalidOperationException($"Cannot refund payment with status: {payment.Status}");
            }

            // Check if refund amount is valid
            var existingRefunds = await _refundRepository.GetByPaymentIdAsync(request.PaymentId);
            var totalRefunded = existingRefunds.Where(r => r.Status == "Completed").Sum(r => r.Amount);
            
            if (totalRefunded + request.Amount > payment.Amount)
            {
                throw new InvalidOperationException($"Refund amount exceeds available refundable amount.");
            }

            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = request.PaymentId,
                Amount = request.Amount,
                Currency = payment.Currency,
                Reason = request.Reason,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _refundRepository.AddAsync(refund);
            await _refundRepository.SaveChangesAsync();

            _logger.LogInformation("Refund created successfully with ID: {RefundId} for Payment: {PaymentId}", refund.Id, request.PaymentId);

            return new RefundDto
            {
                Id = refund.Id,
                PaymentId = refund.PaymentId,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Reason = refund.Reason,
                Status = refund.Status,
                TransactionId = refund.TransactionId,
                GatewayResponse = refund.GatewayResponse,
                ProcessedAt = refund.ProcessedAt,
                FailureReason = refund.FailureReason,
                CreatedAt = refund.CreatedAt,
                UpdatedAt = refund.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment: {PaymentId}", request.PaymentId);
            throw;
        }
    }
}