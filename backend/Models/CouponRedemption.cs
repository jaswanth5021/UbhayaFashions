namespace backend.Models;

public class CouponRedemption
{
    public int Id { get; set; }
    public int CouponId { get; set; }
    public Coupon Coupon { get; set; } = null!;
    public int CustomerId { get; set; }
    public int OrderId { get; set; }
    public DateTime RedeemedAtUtc { get; set; } = DateTime.UtcNow;
}