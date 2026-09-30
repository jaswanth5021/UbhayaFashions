using System.Text.Json.Serialization;

namespace backend.Models;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    [JsonIgnore]
    public Product Product { get; set; } = null!;
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public int Stock { get; set; }
}
