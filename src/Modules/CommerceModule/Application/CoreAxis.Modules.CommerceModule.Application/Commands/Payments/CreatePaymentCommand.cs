using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Payments;

public record CreatePaymentCommand(
    Guid OrderId,
    string PaymentMethod,
    decimal Amount,
    string Currency
) : IRequest<PaymentDto>;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;

    public CreatePaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILogger<CreatePaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<PaymentDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify order exists
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {request.OrderId} not found.");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                PaymentMethod = request.PaymentMethod,
                Status = "Pending",
                Amount = request.Amount,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation("Payment created successfully with ID: {PaymentId} for Order: {OrderId}", payment.Id, request.OrderId);

            return new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                TransactionId = payment.TransactionId,
                GatewayResponse = payment.GatewayResponse,
                ProcessedAt = payment.ProcessedAt,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                Refunds = new List<RefundDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for order: {OrderId}", request.OrderId);
            throw;
        }
    }
}