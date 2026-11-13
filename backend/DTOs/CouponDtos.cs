namespace backend.DTOs;

public record CouponRequest(
    string Code,
    string Name,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaximumDiscount,
    decimal MinimumOrderAmount,
    int? MaximumUses,
    int? PerCustomerLimit,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool IsActive,
    bool IsAutomatic,
    bool FirstOrderOnly,
    int? ProductId,
    int? CategoryId);

public record CouponApplyRequest(string? Code, decimal Subtotal, List<int> ProductIds);

public record CheckoutCouponOffersRequest(List<CreateOrderItem> Items);
