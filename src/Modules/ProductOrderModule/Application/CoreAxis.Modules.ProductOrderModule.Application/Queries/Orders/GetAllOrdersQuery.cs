using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Queries.Orders;

public record GetAllOrdersQuery(
    OrderStatus? Status = null,
    string? AssetCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<OrderDto>>>;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetAllOrdersQueryHandler> _logger;

    public GetAllOrdersQueryHandler(
        IOrderRepository orderRepository,
        ILogger<GetAllOrdersQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all orders, page {PageNumber}, pageSize {PageSize}, status {Status}, assetCode {AssetCode}", 
                request.PageNumber, request.PageSize, request.Status, request.AssetCode);

            var orders = await _orderRepository.GetAllOrdersAsync(
                request.Status,
                request.AssetCode,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var totalCount = await _orderRepository.GetAllOrdersCountAsync(
                request.Status,
                request.AssetCode,
                request.FromDate,
                request.ToDate,
                cancellationToken);

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
                    LineTotal = ol.LineTotal?.Amount ?? 0,
                    Notes = ol.Notes
                }).ToList() ?? new List<OrderLineDto>()
            }).ToList();

            var pagedResult = new PagedResult<OrderDto>
            {
                Items = orderDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Result<PagedResult<OrderDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all orders");
            return Result<PagedResult<OrderDto>>.Failure($"Error getting orders: {ex.Message}");
        }
    }
}