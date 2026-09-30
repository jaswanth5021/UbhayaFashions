namespace LadiesDressStore.Web.Models;

public class ProductViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProductVariantViewModel? StartingVariant => Variants.OrderBy(variant => variant.Price).FirstOrDefault();

    public decimal Price => StartingVariant?.Price ?? 0;

    public decimal Discount => StartingVariant?.Discount ?? 0;

    public int Stock => Variants.Sum(variant => variant.Stock);

    public string Category { get; set; } = string.Empty;

    public string Sizes => string.Join(",", Variants.Select(variant => variant.Size));

    public List<ProductVariantViewModel> Variants { get; set; } = [];

    public string Colors { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public List<ProductImageViewModel> Images { get; set; } = [];

    public List<ProductVideoViewModel> Videos { get; set; } = [];
}

public class ProductVariantViewModel
{
    public int Id { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public int Stock { get; set; }
}

public class ProductImageViewModel
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public class ProductVideoViewModel
{
    public int Id { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
