using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Application.Queries;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Handlers;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        
        if (order == null || order.UserId != request.UserId)
            return null;

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            AssetCode = order.AssetCode.Value,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderLines = order.OrderLines.Select(ol => new OrderLineDto
            {
                Id = ol.Id,
                OrderId = ol.OrderId,
                AssetCode = ol.AssetCode.Value,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                TotalPrice = ol.TotalPrice,
                Description = ol.Description
            }).ToList()
        };
    }
}

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, List<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetUserOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<OrderDto>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetUserOrdersAsync(request.UserId, request.Page, request.PageSize);
        
        return orders.Select(order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            AssetCode = order.AssetCode.Value,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderLines = order.OrderLines.Select(ol => new OrderLineDto
            {
                Id = ol.Id,
                OrderId = ol.OrderId,
                AssetCode = ol.AssetCode.Value,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                TotalPrice = ol.TotalPrice,
                Description = ol.Description
            }).ToList()
        }).ToList();
    }
}

public class GetOrderLinesQueryHandler : IRequestHandler<GetOrderLinesQuery, List<OrderLineDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderLinesQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<OrderLineDto>> Handle(GetOrderLinesQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);
        
        if (order == null || order.UserId != request.UserId)
            return new List<OrderLineDto>();

        return order.OrderLines.Select(ol => new OrderLineDto
        {
            Id = ol.Id,
            OrderId = ol.OrderId,
            AssetCode = ol.AssetCode.Value,
            Quantity = ol.Quantity,
            UnitPrice = ol.UnitPrice,
            TotalPrice = ol.TotalPrice,
            Description = ol.Description
        }).ToList();
    }
}