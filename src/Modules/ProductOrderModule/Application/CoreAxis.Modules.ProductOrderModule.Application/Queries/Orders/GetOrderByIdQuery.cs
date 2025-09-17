using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Queries.Orders;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
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

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting order with ID {OrderId}", request.OrderId);

            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", request.OrderId);
                return Result<OrderDto>.Failure("Order not found");
            }

            var orderDto = new OrderDto
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
            };

            return Result<OrderDto>.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order with ID {OrderId}", request.OrderId);
            return Result<OrderDto>.Failure($"Error getting order: {ex.Message}");
        }
    }
}