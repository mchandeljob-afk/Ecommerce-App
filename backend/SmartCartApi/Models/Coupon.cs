namespace SmartCartApi.Models;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal MinCartSubtotal { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum CouponType
{
    Flat,
    Percentage
}
