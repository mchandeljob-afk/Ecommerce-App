using Microsoft.EntityFrameworkCore;
using SmartCartApi.Data;
using SmartCartApi.Models;

namespace SmartCartApi.Services;

public interface ICouponService
{
    Task<Coupon?> GetCouponByCodeAsync(string code);
    decimal CalculateDiscount(Coupon coupon, decimal subtotal);
    (bool IsValid, string Message) ValidateCoupon(Coupon? coupon, decimal subtotal);
}

public class CouponService : ICouponService
{
    private readonly AppDbContext _context;

    public CouponService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Coupon?> GetCouponByCodeAsync(string code)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant() && c.IsActive);
    }

    public decimal CalculateDiscount(Coupon coupon, decimal subtotal)
    {
        if (subtotal < coupon.MinCartSubtotal)
            return 0;

        return coupon.Type switch
        {
            CouponType.Flat => coupon.DiscountValue,
            CouponType.Percentage => Math.Min(
                subtotal * coupon.DiscountValue / 100m,
                coupon.MaxDiscount ?? decimal.MaxValue
            ),
            _ => 0
        };
    }

    public (bool IsValid, string Message) ValidateCoupon(Coupon? coupon, decimal subtotal)
    {
        if (coupon == null)
            return (false, "Invalid coupon code.");

        if (!coupon.IsActive)
            return (false, "This coupon is no longer active.");

        if (subtotal < coupon.MinCartSubtotal)
            return (false, $"Cart subtotal must be at least ₹{coupon.MinCartSubtotal:F2} to use this coupon. Current subtotal: ₹{subtotal:F2}.");

        return (true, "Coupon applied successfully.");
    }
}
