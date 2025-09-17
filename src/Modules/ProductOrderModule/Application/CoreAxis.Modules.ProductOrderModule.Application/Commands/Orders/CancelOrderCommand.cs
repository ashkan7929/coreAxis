using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Orders;

public record CancelOrderCommand(
    Guid OrderId,
    string? CancellationReason = null
) : IRequest<Result<OrderDto>>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                return Result<OrderDto>.Failure("Order not found");
            }

            // Check if order can be cancelled
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                return Result<OrderDto>.Failure($"Cannot cancel order with status {order.Status}");
            }

            order.Cancel(request.CancellationReason);

            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

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
                OrderLines = order.OrderLines.Select(ol => new OrderLineDto
                {
                    Id = ol.Id,
                    OrderId = ol.OrderId,
                    AssetCode = ol.AssetCode.Value,
                    Quantity = ol.Quantity,
                    UnitPrice = ol.UnitPrice?.Amount ?? 0,
                    LineTotal = ol.LineTotal?.Amount ?? 0,
                    Notes = ol.Notes
                }).ToList()
            };

            return Result<OrderDto>.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", request.OrderId);
            return Result<OrderDto>.Failure("Failed to cancel order");
        }
    }
}