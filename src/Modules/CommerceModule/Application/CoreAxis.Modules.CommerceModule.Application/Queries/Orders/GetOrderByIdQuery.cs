using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Orders;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdWithDetailsAsync(request.Id);
            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", request.Id);
                return null;
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                ShippingAddress = order.ShippingAddress,
                BillingAddress = order.BillingAddress,
                OrderDate = order.OrderDate,
                ShippedDate = order.ShippedDate,
                DeliveredDate = order.DeliveredDate,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.Items?.Select(item => new OrderItemDto
                {
                    Id = item.Id,
                    OrderId = item.OrderId,
                    InventoryItemId = item.InventoryItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = item.Currency,
                    CreatedAt = item.CreatedAt
                }).ToList() ?? new List<OrderItemDto>(),
                Payments = order.Payments?.Select(payment => new PaymentDto
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
                }).ToList() ?? new List<PaymentDto>(),
                DiscountRules = order.DiscountRules?.Select(discount => new DiscountRuleDto
                {
                    Id = discount.Id,
                    Name = discount.Name,
                    DiscountType = discount.DiscountType,
                    DiscountValue = discount.DiscountValue,
                    MinimumAmount = discount.MinimumAmount,
                    MaximumDiscount = discount.MaximumDiscount,
                    Currency = discount.Currency,
                    IsActive = discount.IsActive,
                    ValidFrom = discount.ValidFrom,
                    ValidTo = discount.ValidTo,
                    CreatedAt = discount.CreatedAt,
                    UpdatedAt = discount.UpdatedAt
                }).ToList() ?? new List<DiscountRuleDto>()
            };

            _logger.LogInformation("Retrieved order with ID: {OrderId}", request.Id);
            return orderDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with ID: {OrderId}", request.Id);
            throw;
        }
    }
}