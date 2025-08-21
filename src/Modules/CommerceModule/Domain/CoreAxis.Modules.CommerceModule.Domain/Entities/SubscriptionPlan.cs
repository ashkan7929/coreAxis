using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a subscription plan that defines pricing and billing cycles.
/// </summary>
public class SubscriptionPlan : EntityBase
{
    /// <summary>
    /// Gets or sets the name of the subscription plan.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the subscription plan.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the price of the subscription plan.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the billing interval in days (e.g., 30 for monthly, 365 for yearly).
    /// </summary>
    public int BillingIntervalDays { get; set; }

    /// <summary>
    /// Gets or sets the trial period in days (0 means no trial).
    /// </summary>
    public int TrialPeriodDays { get; set; } = 0;

    /// <summary>
    /// Gets or sets the setup fee for the subscription.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal SetupFee { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this plan is currently active and available for new subscriptions.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this plan is visible to customers.
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of subscriptions allowed for this plan (null = unlimited).
    /// </summary>
    public int? MaxSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the current number of active subscriptions for this plan.
    /// </summary>
    public int CurrentSubscriptions { get; set; } = 0;

    /// <summary>
    /// Gets or sets the features included in this plan as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? FeaturesJson { get; set; }

    /// <summary>
    /// Gets or sets the metadata for this plan as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the sort order for displaying plans.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether cancellation is allowed for this plan.
    /// </summary>
    public bool AllowCancellation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether plan changes are allowed.
    /// </summary>
    public bool AllowPlanChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets the grace period in days after payment failure before cancellation.
    /// </summary>
    public int GracePeriodDays { get; set; } = 3;

    /// <summary>
    /// Navigation property for subscriptions using this plan.
    /// </summary>
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    /// <summary>
    /// Checks if the plan is available for new subscriptions.
    /// </summary>
    public bool IsAvailableForNewSubscriptions()
    {
        if (!IsActive || !IsPublic) return false;
        
        if (MaxSubscriptions.HasValue && CurrentSubscriptions >= MaxSubscriptions.Value)
            return false;
            
        return true;
    }

    /// <summary>
    /// Increments the current subscription count.
    /// </summary>
    public void IncrementSubscriptionCount()
    {
        CurrentSubscriptions++;
    }

    /// <summary>
    /// Decrements the current subscription count.
    /// </summary>
    public void DecrementSubscriptionCount()
    {
        if (CurrentSubscriptions > 0)
            CurrentSubscriptions--;
    }

    /// <summary>
    /// Gets the billing interval description.
    /// </summary>
    public string GetBillingIntervalDescription()
    {
        return BillingIntervalDays switch
        {
            1 => "Daily",
            7 => "Weekly",
            30 => "Monthly",
            90 => "Quarterly",
            365 => "Yearly",
            _ => $"Every {BillingIntervalDays} days"
        };
    }
}