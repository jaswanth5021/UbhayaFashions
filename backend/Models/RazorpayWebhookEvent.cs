namespace backend.Models;

public class RazorpayWebhookEvent
{
    public int Id { get; set; }
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string ProcessingStatus { get; set; } = "Received";
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
