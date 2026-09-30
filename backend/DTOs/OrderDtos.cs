namespace backend.DTOs;

public record CreateOrderItem(
    int ProductId,
    int Quantity,
    string? Size,
    string? Color);

public record CreateOrderRequest(
    string ShippingAddress,
    List<CreateOrderItem> Items);
