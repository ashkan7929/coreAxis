using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a rule for splitting payments between different parties.
/// </summary>
public class SplitPaymentRule : EntityBase
{
    /// <summary>
    /// Gets or sets the name of the split payment rule.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the split payment rule.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of this rule (higher number = higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the conditions for applying this rule as JSON.
    /// This can include product categories, vendor IDs, order amounts, etc.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ConditionsJson { get; set; }

    /// <summary>
    /// Gets or sets the split configuration as JSON.
    /// This defines how the payment should be split between different parties.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string SplitConfigurationJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when this rule becomes effective.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets the date when this rule expires.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Navigation property for split payment allocations using this rule.
    /// </summary>
    public virtual ICollection<SplitPaymentAllocation> Allocations { get; set; } = new List<SplitPaymentAllocation>();

    /// <summary>
    /// Checks if the rule is currently valid based on date range and active status.
    /// </summary>
    public bool IsCurrentlyValid()
    {
        if (!IsActive) return false;
        
        var now = DateTime.UtcNow;
        
        if (EffectiveDate.HasValue && now < EffectiveDate.Value) return false;
        if (ExpiryDate.HasValue && now > ExpiryDate.Value) return false;
        
        return true;
    }

    /// <summary>
    /// Checks if the rule is applicable for the given date.
    /// </summary>
    public bool IsApplicableForDate(DateTime date)
    {
        if (EffectiveDate.HasValue && date < EffectiveDate.Value) return false;
        if (ExpiryDate.HasValue && date > ExpiryDate.Value) return false;
        
        return true;
    }
}