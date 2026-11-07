namespace LadiesDressStore.Web.Models;

public class PaymentCheckoutViewModel
{
    public int OrderId { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public string KeyId { get; set; } = "";
    public long Amount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCharge { get; set; }
    public List<PaymentOrderItemViewModel> Items { get; set; } = [];
    public string Currency { get; set; } = "INR";
    public string Name { get; set; } = "Ubhaya Fashions";
    public string Description { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public bool SaveAddress { get; set; }
    public int SelectedSavedAddressId { get; set; }
    public SaveAddressViewModel Address { get; set; } = new();
}

public class PaymentOrderItemViewModel
{
    public string Name { get; set; } = "";
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class PaymentVerifyViewModel
{
    public int OrderId { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string RazorpaySignature { get; set; } = "";
    public bool SaveAddress { get; set; }
    public int SelectedSavedAddressId { get; set; }
    public SaveAddressViewModel Address { get; set; } = new();
}
