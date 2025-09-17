using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Orders;

public record UpdateOrderCommand(
    Guid OrderId,
    OrderStatus? Status = null,
    decimal? LockedPrice = null,
    DateTime? PriceLockExpiresAt = null,
    string? IdempotencyKey = null
) : IRequest<Result<OrderDto>>;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public UpdateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                return Result<OrderDto>.Failure("Order not found");
            }

            // Only allow updates if order is in Pending status
            if (order.Status != OrderStatus.Pending)
            {
                return Result<OrderDto>.Failure("Only pending orders can be updated");
            }

            // Note: UpdateStatus, SetPriceLock, and SetIdempotencyKey methods need to be implemented in Order entity
            // For now, we'll comment out these calls until the methods are added to the Order class
            
            // if (request.Status.HasValue)
            // {
            //     order.UpdateStatus(request.Status.Value);
            // }

            // if (request.LockedPrice.HasValue && request.PriceLockExpiresAt.HasValue)
            // {
            //     order.SetPriceLock(request.LockedPrice.Value, request.PriceLockExpiresAt.Value);
            // }

            // if (!string.IsNullOrEmpty(request.IdempotencyKey))
            // {
            //     order.SetIdempotencyKey(request.IdempotencyKey);
            // }

            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                AssetCode = order.AssetCode.Value,
                TotalAmount = order.TotalAmount?.Amount ?? 0,
                Status = order.Status.ToString(),
                LockedPrice = order.LockedPrice?.Amount ?? 0,
                CreatedOn = order.CreatedOn,
                LastModifiedOn = order.LastModifiedOn,
                OrderLines = order.OrderLines?.Select(ol => new OrderLineDto
                {
                    Id = ol.Id,
                    AssetCode = ol.AssetCode.Value,
                    Quantity = ol.Quantity,
                    UnitPrice = ol.UnitPrice?.Amount ?? 0,
                    LineTotal = ol.LineTotal?.Amount ?? 0
                }).ToList() ?? new List<OrderLineDto>()
            };

            return Result<OrderDto>.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", request.OrderId);
            return Result<OrderDto>.Failure("Failed to update order");
        }
    }
}