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
                UserId = order.CustomerId,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                DiscountAmount = order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                FinalAmount = order.TotalAmount,
                Notes = order.SpecialInstructions,
                OrderDate = order.OrderDate,
                ShippingDate = null,
                DeliveryDate = order.DeliveredAt,
                CreatedAt = order.CreatedOn,
                UpdatedAt = order.LastModifiedOn,
                OrderItems = order.OrderItems?.Select(item => new OrderItemDto
                {
                    Id = item.Id,
                    OrderId = item.OrderId,
                    InventoryItemId = Guid.Empty,
                    AssetCode = item.ProductSku,
                    ItemName = item.ProductName,
                    Quantity = (decimal)item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = order.Currency,
                    DiscountAmount = item.DiscountAmount,
                    Notes = null,
                    CreatedAt = item.CreatedOn,
                    UpdatedAt = item.LastModifiedOn
                }).ToList() ?? new List<OrderItemDto>(),
                Payment = order.Payments?
                    .OrderByDescending(p => p.ProcessedAt ?? p.CreatedOn)
                    .Select(payment => new PaymentDto
                    {
                        Id = payment.Id,
                        OrderId = payment.OrderId,
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        PaymentMethod = payment.Method.ToString(),
                        Status = payment.Status.ToString(),
                        TransactionId = payment.TransactionId,
                        GatewayResponse = payment.GatewayReference,
                        ProcessedAt = payment.ProcessedAt,
                        FailureReason = payment.FailureReason,
                        CreatedAt = payment.CreatedOn,
                        UpdatedAt = payment.LastModifiedOn,
                        Refunds = payment.Refunds?.Select(refund => new RefundDto
                        {
                            Id = refund.Id,
                            PaymentId = refund.PaymentId,
                            Amount = refund.Amount,
                            Currency = refund.Currency,
                            Reason = refund.Reason,
                            Status = refund.Status.ToString(),
                            TransactionId = refund.TransactionId,
                            GatewayResponse = refund.GatewayResponse,
                            ProcessedAt = refund.ProcessedAt,
                            FailureReason = refund.FailureReason,
                            CreatedAt = refund.CreatedAt,
                            UpdatedAt = refund.UpdatedAt
                        }).ToList() ?? new List<RefundDto>()
                    })
                    .FirstOrDefault(),
                AppliedDiscounts = new List<DiscountRuleDto>()
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