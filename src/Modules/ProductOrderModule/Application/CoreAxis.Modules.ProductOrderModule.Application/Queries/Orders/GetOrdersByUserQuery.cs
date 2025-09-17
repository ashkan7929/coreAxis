using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Queries.Orders;

public record GetOrdersByUserQuery(
    Guid UserId,
    OrderStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<OrderDto>>>;

public class GetOrdersByUserQueryHandler : IRequestHandler<GetOrdersByUserQuery, Result<PagedResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrdersByUserQueryHandler> _logger;

    public GetOrdersByUserQueryHandler(
        IOrderRepository orderRepository,
        ILogger<GetOrdersByUserQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetOrdersByUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting orders for user {UserId}, page {PageNumber}, pageSize {PageSize}", 
                request.UserId, request.PageNumber, request.PageSize);

            var orders = await _orderRepository.GetUserOrdersAsync(request.UserId, request.PageNumber, request.PageSize);
            var totalCount = await _orderRepository.GetUserOrdersCountAsync(request.UserId);

            var orderDtos = orders.Select(order => new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                AssetCode = order.AssetCode.Value,
                TotalAmount = order.TotalAmount?.Amount ?? 0,
                Status = order.Status.ToString(),
                LockedPrice = order.LockedPrice?.Amount,
                CreatedOn = order.CreatedOn,
                LastModifiedOn = order.LastModifiedOn,
                OrderLines = order.OrderLines?.Select(ol => new OrderLineDto
                {
                    Id = ol.Id,
                    OrderId = ol.OrderId,
                    AssetCode = ol.AssetCode.Value,
                    Quantity = ol.Quantity,
                    UnitPrice = ol.UnitPrice?.Amount ?? 0,
                    LineTotal = ol.LineTotal?.Amount ?? 0
                }).ToList() ?? new List<OrderLineDto>()
            }).ToList();

            var pagedResult = new PagedResult<OrderDto>
            {
                Items = orderDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return Result<PagedResult<OrderDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", request.UserId);
            return Result<PagedResult<OrderDto>>.Failure($"Error getting orders: {ex.Message}");
        }
    }
}