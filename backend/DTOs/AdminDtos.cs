namespace backend.DTOs;

public record AdminLoginRequest(string Email, string Password);
public record AdminLoginResponse(string Token, int Id, string Name, string Email);
public class CategoryRequest { public string Name { get; set; } = string.Empty; public bool ShowOnHomePage { get; set; } public string? ImageUrl { get; set; } }

public class AdminProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Colors { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<AdminProductVariantRequest> Variants { get; set; } = [];
}

public class AdminProductVariantRequest
{
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public int Stock { get; set; }
}

public class InventoryAdjustmentRequest
{
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Type { get; set; } = "Adjustment";
    public string? Note { get; set; }
}

public class AdminOrderStatusRequest { public string Status { get; set; } = string.Empty; public string? PaymentStatus { get; set; } }
