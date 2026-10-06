namespace LadiesDressStore.Web.Models;

public class PaymentCheckoutViewModel
{
    public int OrderId { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public string KeyId { get; set; } = "";
    public long Amount { get; set; }
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
