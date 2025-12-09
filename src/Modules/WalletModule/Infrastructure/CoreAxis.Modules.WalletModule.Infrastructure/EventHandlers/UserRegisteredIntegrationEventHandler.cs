using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Infrastructure.EventHandlers;

public class UserRegisteredIntegrationEventHandler : IIntegrationEventHandler<UserRegistered>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        IWalletRepository walletRepository,
        IWalletTypeRepository walletTypeRepository,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
    {
        _walletRepository = walletRepository;
        _walletTypeRepository = walletTypeRepository;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegistered @event)
    {
        _logger.LogInformation("Handling UserRegistered event for UserId: {UserId}", @event.UserId);

        try
        {
            var defaultTypes = await _walletTypeRepository.GetDefaultAsync();

            if (!defaultTypes.Any())
            {
                _logger.LogInformation("No default wallet types configured. Skipping wallet creation for UserId: {UserId}", @event.UserId);
                return;
            }

            foreach (var type in defaultTypes)
            {
                // Check if wallet already exists
                var exists = await _walletRepository.ExistsAsync(@event.UserId, type.Id);
                if (exists)
                {
                    _logger.LogInformation("Default wallet of type {TypeName} already exists for UserId: {UserId}", type.Name, @event.UserId);
                    continue;
                }

                // Create wallet
                var wallet = new Wallet(@event.UserId, type.Id, "USD");
                await _walletRepository.AddAsync(wallet);
                _logger.LogInformation("Created default wallet of type {TypeName} for UserId: {UserId}", type.Name, @event.UserId);
            }

            await _walletRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default wallets for UserId: {UserId}", @event.UserId);
            throw;
        }
    }
}
