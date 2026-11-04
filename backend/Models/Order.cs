namespace backend.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Pending";
    public string PaymentMethod { get; set; } = "COD";
    public DelhiveryShipment? DelhiveryShipment { get; set; }
    public string ShippingAddress { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? RazorpayOrderId { get; set; }
    // Structured address snapshot required if Razorpay notifies us after the customer leaves checkout.
    public string? DeliveryAddressJson { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
