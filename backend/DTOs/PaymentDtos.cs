namespace backend.DTOs;

public record CreatePaymentOrderRequest(
    string ShippingAddress,
    List<CreateOrderItem> Items);

public record VerifyPaymentRequest(
    int OrderId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);
