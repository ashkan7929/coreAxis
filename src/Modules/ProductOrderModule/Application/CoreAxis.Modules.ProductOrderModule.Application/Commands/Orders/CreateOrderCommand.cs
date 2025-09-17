using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Orders;

public record CreateOrderCommand(
    string UserId,
    string AssetCode,
    decimal TotalAmount,
    List<CreateOrderLineDto> OrderLines
) : IRequest<Result<OrderDto>>;

public record CreateOrderLineDto(
    string AssetCode,
    decimal Quantity,
    decimal UnitPrice,
    string? Description = null
);

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var assetCode = AssetCode.Create(request.AssetCode);

        var orderLines = request.OrderLines.Select(ol => 
            OrderLine.Create(Guid.NewGuid(), AssetCode.Create(ol.AssetCode), ol.Quantity, Money.Create(ol.UnitPrice, "USD"))
        ).ToList();

            var order = Order.Create(
                 Guid.Parse(request.UserId),
                 OrderType.Buy, // Default to Buy order type
                 assetCode,
                 request.TotalAmount, // Use decimal quantity instead of Money
                 "default" // Default tenant ID
             );

            await _orderRepository.AddAsync(order);
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
            _logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
            return Result<OrderDto>.Failure($"Failed to create order: {ex.Message}");
        }
    }
}