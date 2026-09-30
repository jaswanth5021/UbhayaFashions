namespace backend.Models;

public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public int? Age { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetExpiresUtc { get; set; }

    public string? PasswordHash { get; set; }

    public string? ProfileImage { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
}
