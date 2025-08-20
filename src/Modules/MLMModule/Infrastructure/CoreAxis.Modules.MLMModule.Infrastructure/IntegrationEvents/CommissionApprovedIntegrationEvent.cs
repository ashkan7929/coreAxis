using CoreAxis.EventBus;
using System;

namespace CoreAxis.Modules.MLMModule.Infrastructure.IntegrationEvents;

/// <summary>
/// Integration event published when a commission is approved and ready for wallet deposit.
/// This event is consumed by the Wallet module to process the commission payment.
/// </summary>
public class CommissionApprovedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// Gets the commission transaction identifier.
    /// </summary>
    public Guid CommissionId { get; }

    /// <summary>
    /// Gets the user identifier who will receive the commission.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the commission amount to be deposited.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the description of the commission transaction.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the reference for the commission transaction.
    /// </summary>
    public string Reference { get; }

    /// <summary>
    /// Gets the idempotency key to ensure the transaction is processed only once.
    /// </summary>
    public string IdempotencyKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionApprovedIntegrationEvent"/> class.
    /// </summary>
    /// <param name="commissionId">The commission transaction identifier.</param>
    /// <param name="userId">The user identifier who will receive the commission.</param>
    /// <param name="amount">The commission amount to be deposited.</param>
    /// <param name="description">The description of the commission transaction.</param>
    /// <param name="reference">The reference for the commission transaction.</param>
    /// <param name="idempotencyKey">The idempotency key to ensure the transaction is processed only once.</param>
    public CommissionApprovedIntegrationEvent(
        Guid commissionId,
        Guid userId,
        decimal amount,
        string description,
        string reference,
        string idempotencyKey)
    {
        CommissionId = commissionId;
        UserId = userId;
        Amount = amount;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        IdempotencyKey = idempotencyKey ?? throw new ArgumentNullException(nameof(idempotencyKey));
    }
}