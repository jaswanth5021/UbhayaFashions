namespace LadiesDressStore.Web.Models;

public static class ImageUrlHelper
{
    public static string Resolve(string? imagePath, string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return string.Empty;

        // Uploaded media is served by the site from its stored /uploads path.
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return absoluteUri.PathAndQuery + absoluteUri.Fragment;

            return absoluteUri.Scheme is "http" or "https"
                ? absoluteUri.AbsoluteUri
                : "data:,";
        }

        return "/" + imagePath.TrimStart('/');
    }
}
