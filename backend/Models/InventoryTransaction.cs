namespace backend.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? Size { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockAfter { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
}
