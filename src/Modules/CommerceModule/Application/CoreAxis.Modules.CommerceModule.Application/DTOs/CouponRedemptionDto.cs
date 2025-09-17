namespace CoreAxis.Modules.CommerceModule.Application.DTOs;

public class CouponRedemptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public string CouponCode { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime RedeemedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}