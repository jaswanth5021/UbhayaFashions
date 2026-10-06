namespace backend.Models;

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public List<ProductReviewImage> Images { get; set; } = [];
}

public class ProductReviewImage
{
    public int Id { get; set; }
    public int ProductReviewId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class ProductReviewVote
{
    public int Id { get; set; }
    public int ProductReviewId { get; set; }
    public int CustomerId { get; set; }
    public int VoteType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
