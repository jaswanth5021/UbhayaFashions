namespace backend.Models;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string DiscountType { get; set; } = "Percentage"; // Percentage or Fixed
    public decimal DiscountValue { get; set; }
    public decimal? MaximumDiscount { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public int? MaximumUses { get; set; }
    public int UsageCount { get; set; }
    public int? PerCustomerLimit { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAutomatic { get; set; }
    public bool FirstOrderOnly { get; set; }
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}