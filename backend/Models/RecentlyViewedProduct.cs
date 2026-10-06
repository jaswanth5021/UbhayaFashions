namespace backend.Models;

public class RecentlyViewedProduct
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime ViewedAtUtc { get; set; } = DateTime.UtcNow;
}