namespace LadiesDressStore.Web.Models;

public sealed class NotificationViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
}

public sealed class NotificationListViewModel
{
    public int UnreadCount { get; set; }
    public List<NotificationViewModel> Items { get; set; } = [];
}
