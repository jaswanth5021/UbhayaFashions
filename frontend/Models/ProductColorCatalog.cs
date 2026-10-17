namespace LadiesDressStore.Web.Models;

public static class ProductColorCatalog
{
    public static readonly (string Name, string Hex)[] All =
    [
        ("Black", "#171717"),
        ("White", "#ffffff"),
        ("Red", "#b3263b"),
        ("Pink", "#e98aa9"),
        ("Green", "#72a66a"),
        ("Blue", "#6288c8"),
        ("Yellow", "#e7b84c"),
        ("Maroon", "#68142e"),
        ("Purple", "#8b68ad"),
        ("Beige", "#d8c2a7"),
        ("Orange", "#e77932"),
        ("Gold", "#c4a15a")
    ];

    public static HashSet<string> Parse(string? colors) =>
        new(Split(colors), StringComparer.OrdinalIgnoreCase);

    public static string Format(IEnumerable<string>? values)
    {
        var selected = new HashSet<string>(
            (values ?? []).SelectMany(Split).Select(Canonical),
            StringComparer.OrdinalIgnoreCase);
        var ordered = All
            .Where(color => selected.Contains(color.Name))
            .Select(color => color.Name)
            .ToList();
        ordered.AddRange(selected
            .Where(value => !All.Any(color => color.Name.Equals(value, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        return string.Join(",", ordered);
    }

    public static string Canonical(string value)
    {
        var compact = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var known = All.FirstOrDefault(color => color.Name.Equals(compact, StringComparison.OrdinalIgnoreCase));
        if (known.Name is not null) return known.Name;
        return string.Join(' ', compact.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length <= 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    public static IEnumerable<string> Split(string? colors) =>
        (colors ?? string.Empty)
            // Use an explicit char array to resolve CS0121 between the char[] and string Split overloads.
            .Split(new char[] { ',', '|', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Canonical);
}
