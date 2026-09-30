namespace backend.Models;

public class ExternalLogin
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Provider { get; set; } = "";
    public string ProviderUserId { get; set; } = "";
    public string? Email { get; set; }
}
