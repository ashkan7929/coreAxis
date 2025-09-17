using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

public interface ICouponRepository
{
    Task<CouponRedemption?> GetCouponRedemptionByIdAsync(Guid id);
    Task<CouponRedemption?> GetCouponRedemptionByCouponCodeAsync(string couponCode, Guid userId);
    Task<(List<CouponRedemption> Redemptions, int TotalCount)> GetCouponRedemptionsAsync(
        Guid? userId = null,
        Guid? orderId = null,
        string? couponCode = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<List<CouponRedemption>> GetRedemptionsByUserIdAsync(Guid userId);
    Task<List<CouponRedemption>> GetRedemptionsByOrderIdAsync(Guid orderId);
    Task<CouponRedemption> AddCouponRedemptionAsync(CouponRedemption redemption);
    Task<CouponRedemption> UpdateCouponRedemptionAsync(CouponRedemption redemption);
    Task<bool> DeleteCouponRedemptionAsync(Guid id);
    Task<bool> CouponRedemptionExistsAsync(Guid id);
    Task<bool> IsCouponAlreadyRedeemedAsync(string couponCode, Guid userId);
    Task<int> GetCouponUsageCountAsync(string couponCode);
    Task<decimal> GetTotalDiscountByCouponCodeAsync(string couponCode);
}