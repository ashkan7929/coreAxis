using CoreAxis.Modules.CommerceModule.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.CommerceModule.Api.DTOs;

/// <summary>
/// DTO for subscription information
/// </summary>
public class SubscriptionDto
{
    /// <summary>
    /// Gets or sets the subscription ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the plan ID
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Gets or sets the subscription status
    /// </summary>
    public SubscriptionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the next billing date
    /// </summary>
    public DateTime NextBillingDate { get; set; }

    /// <summary>
    /// Gets or sets the subscription amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the billing cycle
    /// </summary>
    public BillingCycle BillingCycle { get; set; }

    /// <summary>
    /// Gets or sets the cancellation reason
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Gets or sets the pause until date
    /// </summary>
    public DateTime? PauseUntil { get; set; }

    /// <summary>
    /// Gets or sets the created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new subscription
/// </summary>
public class CreateSubscriptionDto
{
    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the plan ID
    /// </summary>
    [Required]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Gets or sets the start date (optional, defaults to now)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the subscription amount
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the billing cycle
    /// </summary>
    [Required]
    public BillingCycle BillingCycle { get; set; }
}

/// <summary>
/// DTO for updating an existing subscription
/// </summary>
public class UpdateSubscriptionDto
{
    /// <summary>
    /// Gets or sets the plan ID
    /// </summary>
    public Guid? PlanId { get; set; }

    /// <summary>
    /// Gets or sets the subscription amount
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Gets or sets the billing cycle
    /// </summary>
    public BillingCycle? BillingCycle { get; set; }

    /// <summary>
    /// Gets or sets the next billing date
    /// </summary>
    public DateTime? NextBillingDate { get; set; }
}

/// <summary>
/// DTO for cancelling a subscription
/// </summary>
public class CancelSubscriptionDto
{
    /// <summary>
    /// Gets or sets the cancellation reason
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to cancel immediately or at the end of billing period
    /// </summary>
    public bool CancelImmediately { get; set; } = false;
}

/// <summary>
/// DTO for pausing a subscription
/// </summary>
public class PauseSubscriptionDto
{
    /// <summary>
    /// Gets or sets the date until which the subscription should be paused
    /// </summary>
    [Required]
    public DateTime PauseUntil { get; set; }
}