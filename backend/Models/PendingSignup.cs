namespace backend.Models;

public class PendingSignup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Mobile { get; set; } = "";
    public int Age { get; set; }
    public string PasswordHash { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
    public DateTime LastSentUtc { get; set; }
    public int FailedAttempts { get; set; }
}
