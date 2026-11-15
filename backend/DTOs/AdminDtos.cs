namespace backend.DTOs;

public record AdminLoginRequest(string Email, string Password);
public record AdminLoginResponse(string Token, int Id, string Name, string Email);
public record AdminChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminForgotPasswordRequest(string Email);
public record AdminResetPasswordRequest(string Email, string Token, string NewPassword);
public class AdminAccountRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
}
public class CategoryRequest { public string Name { get; set; } = string.Empty; public bool ShowOnHomePage { get; set; } public string? ImageUrl { get; set; } }

public class AdminProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Colors { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsBestSeller { get; set; }
    public List<AdminProductVariantRequest> Variants { get; set; } = [];
}

public class AdminProductVariantRequest
{
    public string? Color { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public int Stock { get; set; }
}

public class ProductSizeChartRequest
{
    public string BrandName { get; set; } = string.Empty;
    public string Caption { get; set; } = "GARMENT MEASUREMENTS";
    public string Unit { get; set; } = "in";
    public List<string> Sizes { get; set; } = [];
    public List<ProductSizeChartRowRequest> Rows { get; set; } = [];
}

public class ProductSizeChartRowRequest
{
    public string Label { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
    public List<string> ValuesIn { get; set; } = [];
    public List<string> ValuesCm { get; set; } = [];
}

public static class ProductSizeChartUnits
{
    public static string? NormalizeUnit(string? unit)
    {
        var normalized = unit?.Trim();
        if (string.Equals(normalized, "in", StringComparison.OrdinalIgnoreCase)) return "in";
        if (string.Equals(normalized, "cm", StringComparison.OrdinalIgnoreCase)) return "cm";
        return null;
    }

    public static void Normalize(ProductSizeChartRequest chart)
    {
        chart.Sizes ??= [];
        chart.Rows ??= [];
        chart.Unit = NormalizeUnit(chart.Unit) ?? "in";
        foreach (var row in chart.Rows.Where(row => row is not null))
            NormalizeRow(row, chart.Unit, chart.Sizes.Count);
    }

    public static void NormalizeRow(ProductSizeChartRowRequest row, string unit, int sizeCount)
    {
        row.Values ??= [];
        row.ValuesIn ??= [];
        row.ValuesCm ??= [];

        if (row.ValuesIn.Count == 0 && row.ValuesCm.Count == 0 && row.Values.Count > 0)
        {
            if (string.Equals(unit, "cm", StringComparison.OrdinalIgnoreCase)) row.ValuesCm = Fit(row.Values, sizeCount);
            else row.ValuesIn = Fit(row.Values, sizeCount);
        }

        row.ValuesIn = Fit(row.ValuesIn, sizeCount);
        row.ValuesCm = Fit(row.ValuesCm, sizeCount);
        row.Values = (string.Equals(unit, "cm", StringComparison.OrdinalIgnoreCase) ? row.ValuesCm : row.ValuesIn).ToList();
    }

    private static List<string> Fit(List<string>? values, int sizeCount)
        => Enumerable.Range(0, sizeCount)
            .Select(index => index < (values?.Count ?? 0) ? values![index] ?? string.Empty : string.Empty)
            .ToList();
}

public class CopyProductSizeChartRequest
{
    public int TargetProductId { get; set; }
}

public class InventoryAdjustmentRequest
{
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Type { get; set; } = "Adjustment";
    public string? Note { get; set; }
}

public class AdminOrderStatusRequest { public string Status { get; set; } = string.Empty; public string? PaymentStatus { get; set; } }
