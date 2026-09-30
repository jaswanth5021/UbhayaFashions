namespace backend.Models;

public class WishlistItem
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}
