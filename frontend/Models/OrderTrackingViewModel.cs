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
    public DateTimeOffset? ShipmentCreatedUtc { get; set; }
    public string? PickupLocation { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string? FirstItemName { get; set; }
    public string? FirstItemImageUrl { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeliveryAddress { get; set; }
}