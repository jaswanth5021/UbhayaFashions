namespace LadiesDressStore.Web.Models;

public sealed class StorefrontBannerViewModel
{
    public string FileName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string ImageUrl { get; set; } = "";
}
