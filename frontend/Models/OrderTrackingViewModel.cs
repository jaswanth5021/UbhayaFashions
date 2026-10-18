namespace LadiesDressStore.Web.Models;

public sealed class OrderTrackingViewModel
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public DateTimeOffset OrderDate { get; set; }
    public string ShippingStatus { get; set; } = "NotCreated";
    public string? Waybill { get; set; }
    public DateTimeOffset? LastTrackingUpdateUtc { get; set; }
}