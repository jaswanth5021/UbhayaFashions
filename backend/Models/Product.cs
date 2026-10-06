namespace backend.Models;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    [JsonIgnore]
    public Category CategoryNavigation { get; set; } = null!;
    private string? _categoryName;
    [NotMapped]
    public string Category
    {
        get => CategoryNavigation?.Name ?? _categoryName ?? string.Empty;
        set => _categoryName = value;
    }
    public string Colors { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsBestSeller { get; set; }
    [NotMapped]
    public decimal? AverageRating { get; set; }
    [NotMapped]
    public int? ReviewCount { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVideo> Videos { get; set; } = new List<ProductVideo>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
