namespace backend.DTOs;

public record CreatePaymentOrderRequest(
    string ShippingAddress,
    List<CreateOrderItem> Items);

public record PaymentDeliveryAddress(
    string Name,
    string Mobile,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country = "India");

public record VerifyPaymentRequest(
    int OrderId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature,
    PaymentDeliveryAddress? DeliveryAddress = null);
