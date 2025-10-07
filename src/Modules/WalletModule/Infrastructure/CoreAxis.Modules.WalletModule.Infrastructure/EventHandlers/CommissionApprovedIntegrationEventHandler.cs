using CoreAxis.EventBus;
using CoreAxis.Modules.MLMModule.Infrastructure.IntegrationEvents;
using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.WalletModule.Infrastructure.EventHandlers;

/// <summary>
/// Handles CommissionApprovedIntegrationEvent by crediting the user's Commission wallet.
/// Ensures idempotency and records transaction with COMMISSION type.
/// </summary>
public class CommissionApprovedIntegrationEventHandler : IIntegrationEventHandler<CommissionApprovedIntegrationEvent>
{
    private readonly ITransactionService _transactionService;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly ILogger<CommissionApprovedIntegrationEventHandler> _logger;

    public CommissionApprovedIntegrationEventHandler(
        ITransactionService transactionService,
        IWalletRepository walletRepository,
        IWalletTypeRepository walletTypeRepository,
        ILogger<CommissionApprovedIntegrationEventHandler> logger)
    {
        _transactionService = transactionService;
        _walletRepository = walletRepository;
        _walletTypeRepository = walletTypeRepository;
        _logger = logger;
    }

    public async Task HandleAsync(CommissionApprovedIntegrationEvent @event)
    {
        try
        {
            _logger.LogInformation(
                "Handling CommissionApprovedIntegrationEvent: CommissionId={CommissionId}, UserId={UserId}, Amount={Amount}",
                @event.CommissionId, @event.UserId, @event.Amount);

            // Resolve Commission wallet type
            var commissionType = await _walletTypeRepository.GetByNameAsync("Commission");
            if (commissionType == null)
            {
                _logger.LogError("Commission wallet type not configured. Cannot credit commission for user {UserId}", @event.UserId);
                throw new InvalidOperationException("Commission wallet type not configured");
            }

            // Get or create user's Commission wallet
            var wallet = await _walletRepository.GetByUserAndTypeAsync(@event.UserId, commissionType.Id);
            if (wallet == null)
            {
                wallet = new Wallet(@event.UserId, commissionType.Id);
                await _walletRepository.AddAsync(wallet);
                await _walletRepository.SaveChangesAsync();
                _logger.LogInformation("Created Commission wallet {WalletId} for user {UserId}", wallet.Id, @event.UserId);
            }

            // Security log and guard for locked wallet
            if (wallet.IsLocked)
            {
                _logger.LogWarning(
                    "Security: Attempt to credit locked wallet. code=WLT_ACCOUNT_FROZEN userId={UserId} walletId={WalletId} reason={Reason}",
                    @event.UserId, wallet.Id, wallet.LockReason);
                throw new InvalidOperationException($"Wallet is locked: {wallet.LockReason}");
            }

            // Execute commission credit with idempotency
            var metadata = new { CommissionId = @event.CommissionId };
            var transaction = await _transactionService.ExecuteCommissionCreditAsync(
                wallet.Id,
                @event.Amount,
                @event.Description,
                @event.Reference,
                metadata,
                @event.IdempotencyKey,
                null);

            _logger.LogInformation(
                "Commission credited. userId={UserId} walletId={WalletId} transactionId={TransactionId}",
                @event.UserId, wallet.Id, transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling CommissionApprovedIntegrationEvent. commissionId={CommissionId} userId={UserId}",
                @event.CommissionId, @event.UserId);
            throw;
        }
    }
}