namespace backend.Models;

public class CustomerNotificationState
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int NotificationId { get; set; }
    public StoreNotification Notification { get; set; } = null!;
    public DateTime? ReadDate { get; set; }
    public DateTime? DismissedDate { get; set; }
}
