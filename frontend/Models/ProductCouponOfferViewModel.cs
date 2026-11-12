namespace LadiesDressStore.Web.Models;

public class ProductCouponOfferViewModel
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public string DiscountText { get; set; } = "";
    public decimal MinimumOrderAmount { get; set; }
    public bool IsAutomatic { get; set; }
    public bool FirstOrderOnly { get; set; }
    public string AppliesTo { get; set; } = "";
    public DateTime? EndsAtUtc { get; set; }
    public string Terms { get; set; } = "";
}
