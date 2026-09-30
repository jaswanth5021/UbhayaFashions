namespace LadiesDressStore.Web.Models;

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
}
