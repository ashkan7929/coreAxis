using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ProductOrderModule.Application.Commands.Orders;

public record DeleteOrderCommand(Guid OrderId) : IRequest<Result<bool>>;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Result<bool>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteOrderCommandHandler> _logger;

    public DeleteOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                return Result<bool>.Failure("Order not found");
            }

            // Only allow deletion of cancelled or draft orders
            if (order.Status != Domain.Enums.OrderStatus.Cancelled && 
                order.Status != Domain.Enums.OrderStatus.Pending)
            {
                return Result<bool>.Failure("Only cancelled or pending orders can be deleted");
            }

            await _orderRepository.DeleteAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order deleted successfully with ID: {OrderId}", request.OrderId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", request.OrderId);
            return Result<bool>.Failure("Failed to delete order");
        }
    }
}