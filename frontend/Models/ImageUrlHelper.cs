namespace LadiesDressStore.Web.Models;

public static class ImageUrlHelper
{
    public static string Resolve(string? imagePath, string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return string.Empty;
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteUri)) return absoluteUri.ToString();

        var baseUri = Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri
            : new Uri("https://localhost:56738/");
        return new Uri(baseUri, imagePath.TrimStart('/')).ToString();
    }
}
