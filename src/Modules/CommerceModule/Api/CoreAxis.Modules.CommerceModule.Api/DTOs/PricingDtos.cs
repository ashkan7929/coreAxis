using System;
using System.Collections.Generic;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;

namespace CoreAxis.Modules.CommerceModule.Api.DTOs;

public class PricingCalculateRequestDto
{
    public OrderSnapshotDto Order { get; set; } = new();
    public List<string>? CouponCodes { get; set; }
    public string? CorrelationId { get; set; }
}

public class OrderSnapshotDto
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItemSnapshotDto> Items { get; set; } = new();
    public decimal SubtotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public Dictionary<string, object>? Metadata { get; set; }
}

public class OrderItemSnapshotDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public List<Guid>? CategoryIds { get; set; }
}

public class PricingResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public BasePricingDto? BasePricing { get; set; }
    public List<AppliedDiscountDto> AppliedDiscounts { get; set; } = new();
    public FinalPricingDto? FinalPricing { get; set; }
    public List<ValidCouponDto> ValidCoupons { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string? CorrelationId { get; set; }
}

public class BasePricingDto
{
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<LineItemPricingDto> LineItemPricing { get; set; } = new();
}

public class FinalPricingDto
{
    public decimal SubtotalAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<AppliedDiscountDto> AppliedDiscounts { get; set; } = new();
}

public class LineItemPricingDto
{
    public Guid LineItemId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxAmount { get; set; }
}

public class AppliedDiscountDto
{
    public Guid DiscountId { get; set; }
    public string DiscountName { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AppliedToAmount { get; set; }
    public int Priority { get; set; }
    public string? CouponCode { get; set; }
}

public class ValidCouponDto
{
    public string CouponCode { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}