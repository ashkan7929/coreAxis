using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Application.Commands.Inventory;

public record DeleteInventoryItemCommand(Guid Id) : IRequest<bool>;

public class DeleteInventoryItemCommandHandler : IRequestHandler<DeleteInventoryItemCommand, bool>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<DeleteInventoryItemCommandHandler> _logger;

    public DeleteInventoryItemCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<DeleteInventoryItemCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var inventoryItem = await _inventoryRepository.GetByIdAsync(request.Id);
            if (inventoryItem == null)
            {
                _logger.LogWarning("Inventory item with ID {InventoryItemId} not found for deletion", request.Id);
                return false;
            }

            // Check if item has reserved quantity or is referenced in orders
            if (inventoryItem.ReservedQuantity > 0)
            {
                throw new InvalidOperationException($"Cannot delete inventory item with ID {request.Id} because it has reserved quantity.");
            }

            await _inventoryRepository.DeleteAsync(inventoryItem);
            await _inventoryRepository.SaveChangesAsync();

            _logger.LogInformation("Inventory item deleted successfully with ID: {InventoryItemId}", request.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item with ID: {InventoryItemId}", request.Id);
            throw;
        }
    }
}