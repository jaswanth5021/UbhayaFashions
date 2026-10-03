namespace LadiesDressStore.Web.Models;

public static class ImageUrlHelper
{
    public static string Resolve(string? imagePath, string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return string.Empty;

        // Media is served by the site from its stored path. Ignore legacy
        // external URLs so they are not mistaken for local /photo-... paths.
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                ? absoluteUri.PathAndQuery + absoluteUri.Fragment
                : "data:,";
        }

        return "/" + imagePath.TrimStart('/');
    }
}
