namespace backend.Models;

public class ProductVideo
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
