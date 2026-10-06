namespace LadiesDressStore.Web.Models;

public class CreateOrderViewModel
{
    public string ShippingAddress { get; set; } = "";
}


public class MyOrderViewModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public DateTimeOffset CreatedDate { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public List<MyOrderItemViewModel> Items { get; set; } = [];
}

public class MyOrderItemViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Size { get; set; } = "";
    public string? Color { get; set; }
}
