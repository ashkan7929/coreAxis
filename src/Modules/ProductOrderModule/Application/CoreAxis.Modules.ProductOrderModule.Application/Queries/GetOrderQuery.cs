using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.ProductOrderModule.Application.Queries;

public class GetOrderQuery : IRequest<OrderDto?>
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class GetUserOrdersQuery : IRequest<List<OrderDto>>
{
    public string UserId { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetOrderLinesQuery : IRequest<List<OrderLineDto>>
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
}