using CoreAxis.Modules.CommerceModule.Application.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Queries.Orders;

public record GetOrdersQuery(
    Guid? UserId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<(List<OrderDto> Orders, int TotalCount)>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, (List<OrderDto> Orders, int TotalCount)>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrdersQueryHandler> _logger;

    public GetOrdersQueryHandler(
        IOrderRepository orderRepository,
        ILogger<GetOrdersQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<(List<OrderDto> Orders, int TotalCount)> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (orders, totalCount) = await _orderRepository.GetOrdersAsync(
                request.UserId,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.MinAmount,
                request.MaxAmount,
                request.PageNumber,
                request.PageSize);

            var orderDtos = orders.Select(order => new OrderDto
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
                    UpdatedAt = payment.UpdatedAt
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
            }).ToList();

            _logger.LogInformation("Retrieved {Count} orders out of {TotalCount} total orders", 
                orderDtos.Count, totalCount);

            return (orderDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            throw;
        }
    }
}