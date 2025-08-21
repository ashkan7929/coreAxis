using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a customer's subscription to a plan.
/// </summary>
public class Subscription : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the subscription plan.
    /// </summary>
    public Guid SubscriptionPlanId { get; set; }

    /// <summary>
    /// Gets or sets the subscription plan.
    /// </summary>
    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the customer who owns this subscription.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the subscription.
    /// </summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// Gets or sets the date when the subscription started.
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date when the subscription ends (null for ongoing subscriptions).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the trial period ends (null if no trial).
    /// </summary>
    public DateTime? TrialEndDate { get; set; }

    /// <summary>
    /// Gets or sets the date of the next billing cycle.
    /// </summary>
    public DateTime NextBillingDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the subscription was last billed.
    /// </summary>
    public DateTime? LastBillingDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the subscription was canceled (null if not canceled).
    /// </summary>
    public DateTime? CanceledDate { get; set; }

    /// <summary>
    /// Gets or sets the reason for cancellation.
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Gets or sets whether the subscription should be canceled at the end of the current billing period.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; } = false;

    /// <summary>
    /// Gets or sets the current billing amount (may differ from plan price due to discounts).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the number of billing cycles completed.
    /// </summary>
    public int BillingCyclesCompleted { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum number of billing cycles (null for unlimited).
    /// </summary>
    public int? MaxBillingCycles { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failed payment attempts.
    /// </summary>
    public int FailedPaymentAttempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum allowed failed payment attempts before cancellation.
    /// </summary>
    public int MaxFailedPaymentAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the date when the subscription enters grace period due to payment failure.
    /// </summary>
    public DateTime? GracePeriodStartDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the grace period ends.
    /// </summary>
    public DateTime? GracePeriodEndDate { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the external subscription ID from payment provider.
    /// </summary>
    [MaxLength(100)]
    public string? ExternalSubscriptionId { get; set; }

    /// <summary>
    /// Navigation property for subscription invoices.
    /// </summary>
    public virtual ICollection<SubscriptionInvoice> Invoices { get; set; } = new List<SubscriptionInvoice>();

    /// <summary>
    /// Checks if the subscription is currently in trial period.
    /// </summary>
    public bool IsInTrial()
    {
        return TrialEndDate.HasValue && DateTime.UtcNow <= TrialEndDate.Value;
    }

    /// <summary>
    /// Checks if the subscription is active and not canceled.
    /// </summary>
    public bool IsActive()
    {
        return Status == SubscriptionStatus.Active && !CanceledDate.HasValue;
    }

    /// <summary>
    /// Checks if the subscription is in grace period.
    /// </summary>
    public bool IsInGracePeriod()
    {
        return GracePeriodStartDate.HasValue && 
               GracePeriodEndDate.HasValue && 
               DateTime.UtcNow >= GracePeriodStartDate.Value && 
               DateTime.UtcNow <= GracePeriodEndDate.Value;
    }

    /// <summary>
    /// Cancels the subscription immediately.
    /// </summary>
    public void Cancel(string reason)
    {
        Status = SubscriptionStatus.Canceled;
        CanceledDate = DateTime.UtcNow;
        CancellationReason = reason;
        EndDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Schedules the subscription for cancellation at the end of the current billing period.
    /// </summary>
    public void CancelAtPeriodEndDate(string reason)
    {
        CancelAtPeriodEnd = true;
        CancellationReason = reason;
    }

    /// <summary>
    /// Marks the subscription as past due and starts grace period.
    /// </summary>
    public void MarkAsPastDue(int gracePeriodDays)
    {
        Status = SubscriptionStatus.PastDue;
        FailedPaymentAttempts++;
        GracePeriodStartDate = DateTime.UtcNow;
        GracePeriodEndDate = DateTime.UtcNow.AddDays(gracePeriodDays);
    }

    /// <summary>
    /// Reactivates the subscription after successful payment.
    /// </summary>
    public void Reactivate()
    {
        Status = SubscriptionStatus.Active;
        FailedPaymentAttempts = 0;
        GracePeriodStartDate = null;
        GracePeriodEndDate = null;
    }

    /// <summary>
    /// Advances to the next billing cycle.
    /// </summary>
    public void AdvanceBillingCycle(int intervalDays)
    {
        LastBillingDate = NextBillingDate;
        NextBillingDate = NextBillingDate.AddDays(intervalDays);
        BillingCyclesCompleted++;
        
        // Check if max billing cycles reached
        if (MaxBillingCycles.HasValue && BillingCyclesCompleted >= MaxBillingCycles.Value)
        {
            Cancel("Maximum billing cycles reached");
        }
    }
}