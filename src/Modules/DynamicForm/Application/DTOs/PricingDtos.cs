using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Application.DTOs
{
    public class PricingCalculationRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid FormId { get; set; }

        public string? FormulaName { get; set; }

        public int? Version { get; set; }

        [Required]
        public Dictionary<string, object?> FormData { get; set; } = new();

        public Dictionary<string, object?>? ExternalDataSources { get; set; }

        public string Currency { get; set; } = "USD";

        public string? Locale { get; set; }
    }

    public class PricingBreakdownItem
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    public class PricingResultDto
    {
        public decimal FinalPrice { get; set; }
        public List<PricingBreakdownItem> Breakdown { get; set; } = new();
        public int FormulaVersion { get; set; }
        public Dictionary<string, object?> UsedData { get; set; } = new();
        public string Currency { get; set; } = "USD";
    }

    public class QuoteResponseDto
    {
        public Guid QuoteId { get; set; }
        public Guid ProductId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public PricingResultDto Pricing { get; set; } = new();
        public Dictionary<string, object?> InputsSnapshot { get; set; } = new();
        public Dictionary<string, object?> ExternalDataSnapshot { get; set; } = new();
    }
}