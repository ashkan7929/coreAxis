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
                        UpdatedAt = payment.LastModifiedOn
                    })
                    .FirstOrDefault(),
                AppliedDiscounts = new List<DiscountRuleDto>()
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