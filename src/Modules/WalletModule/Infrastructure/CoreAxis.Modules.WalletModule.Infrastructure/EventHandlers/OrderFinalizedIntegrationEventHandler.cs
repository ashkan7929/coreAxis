using CoreAxis.EventBus;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.WalletModule.Infrastructure.EventHandlers;

/// <summary>
/// Consumes OrderFinalized events to create Pending commission transactions in Wallet.
/// Delegates commission calculation to MLM service, then records wallet transactions with idempotency.
/// </summary>
public class OrderFinalizedIntegrationEventHandler : IIntegrationEventHandler<OrderFinalized>
{
    private readonly ICommissionCalculationService _commissionCalculationService;
    private readonly ITransactionService _transactionService;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTypeRepository _walletTypeRepository;
    private readonly ILogger<OrderFinalizedIntegrationEventHandler> _logger;

    public OrderFinalizedIntegrationEventHandler(
        ICommissionCalculationService commissionCalculationService,
        ITransactionService transactionService,
        IWalletRepository walletRepository,
        IWalletTypeRepository walletTypeRepository,
        ILogger<OrderFinalizedIntegrationEventHandler> logger)
    {
        _commissionCalculationService = commissionCalculationService;
        _transactionService = transactionService;
        _walletRepository = walletRepository;
        _walletTypeRepository = walletTypeRepository;
        _logger = logger;
    }

    public async Task HandleAsync(OrderFinalized @event)
    {
        _logger.LogInformation(
            "Handling OrderFinalized: OrderId={OrderId}, UserId={UserId}, Amount={Amount}, Currency={Currency}",
            @event.OrderId, @event.UserId, @event.TotalAmount, @event.Currency);

        // Step 1: Ask MLM to compute commissions based on finalized order
        var commissionsResult = await _commissionCalculationService.ProcessPaymentConfirmedAsync(
            sourcePaymentId: @event.OrderId,
            productId: Guid.Empty,
            amount: @event.TotalAmount,
            buyerUserId: @event.UserId,
            correlationId: @event.CorrelationId.ToString(),
            cancellationToken: default);

        if (!commissionsResult.IsSuccess)
        {
            _logger.LogWarning("Commission calculation failed for OrderId={OrderId}. Errors={Errors}",
                @event.OrderId, string.Join("; ", commissionsResult.Errors));
            return; // No wallet transactions created if calculation fails
        }

        if (commissionsResult.Value == null || commissionsResult.Value.Count == 0)
        {
            _logger.LogInformation("No commissions generated for OrderId={OrderId}", @event.OrderId);
            return;
        }

        // Resolve Commission wallet type once
        var commissionWalletType = await _walletTypeRepository.GetByNameAsync("Commission");
        if (commissionWalletType == null)
        {
            _logger.LogError("Commission wallet type not configured. Cannot create wallet transactions for OrderId={OrderId}", @event.OrderId);
            throw new InvalidOperationException("Commission wallet type not configured");
        }

        foreach (var commission in commissionsResult.Value)
        {
            try
            {
                // Get or create user's Commission wallet
                var wallet = await _walletRepository.GetByUserAndTypeAsync(commission.UserId, commissionWalletType.Id);
                if (wallet == null)
                {
                    wallet = new Wallet(commission.UserId, commissionWalletType.Id);
                    await _walletRepository.AddAsync(wallet);
                    await _walletRepository.SaveChangesAsync();
                    _logger.LogInformation("Created Commission wallet {WalletId} for user {UserId}", wallet.Id, commission.UserId);
                }

                if (wallet.IsLocked)
                {
                    _logger.LogWarning(
                        "Security: Attempt to credit locked wallet. code=WLT_ACCOUNT_FROZEN userId={UserId} walletId={WalletId} reason={Reason}",
                        commission.UserId, wallet.Id, wallet.LockReason);
                    throw new InvalidOperationException($"Wallet is locked: {wallet.LockReason}");
                }

                // Prepare idempotency key per beneficiary and order
                var idempotencyKey = $"commission:{@event.OrderId}:{commission.UserId}:L{commission.Level}";

                // Metadata includes commission details and source info
                var metadata = new
                {
                    SourceOrderId = @event.OrderId,
                    commission.Level,
                    commission.Percentage,
                    commission.SourceAmount,
                    CommissionTransactionId = commission.Id,
                    TenantId = @event.TenantId
                };

                // Execute commission credit – creates Pending transaction of type COMMISSION
                var txn = await _transactionService.ExecuteCommissionCreditAsync(
                    wallet.Id,
                    commission.Amount,
                    description: $"Commission L{commission.Level} for Order {@event.OrderId}",
                    reference: @event.OrderId.ToString(),
                    metadata: metadata,
                    idempotencyKey: idempotencyKey,
                    correlationId: @event.CorrelationId.ToString());

                _logger.LogInformation(
                    "Commission pending txn created. userId={UserId} walletId={WalletId} txnId={TransactionId} amount={Amount}",
                    commission.UserId, wallet.Id, txn.Id, commission.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating commission transaction for user {UserId} on OrderId={OrderId}",
                    commission.UserId, @event.OrderId);
                // Continue with other commissions to avoid partial failure blocking all
            }
        }
    }
}