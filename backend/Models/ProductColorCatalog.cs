namespace backend.Models;

public static class ProductColorCatalog
{
    public static readonly string[] Names =
    [
        "Black", "White", "Red", "Pink", "Green", "Blue",
        "Yellow", "Maroon", "Purple", "Beige", "Orange", "Gold"
    ];

    public static string Normalize(string? colors)
    {
        var selected = Split(colors).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ordered = Names.Where(selected.Contains).ToList();
        ordered.AddRange(selected
            .Where(value => !Names.Contains(value, StringComparer.OrdinalIgnoreCase))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        return string.Join(",", ordered);
    }

    public static string Canonical(string value)
    {
        var compact = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var known = Names.FirstOrDefault(name => name.Equals(compact, StringComparison.OrdinalIgnoreCase));
        if (known is not null) return known;
        return string.Join(' ', compact.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length <= 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    public static bool Matches(string? storedColors, string? selectedColor)
    {
        if (string.IsNullOrWhiteSpace(selectedColor)) return true;
        var token = "," + Canonical(selectedColor).Replace(" ", "") + ",";
        var stored = "," + string.Join(",", Split(storedColors)).Replace(" ", "") + ",";
        return stored.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    public static int PriceCeiling(IEnumerable<decimal> prices, int step = 100, int fallback = 10000)
    {
        var highest = prices.DefaultIfEmpty(0).Max();
        if (highest <= 0 || step <= 0) return fallback;
        var rounded = (int)Math.Ceiling(highest / step) * step;
        return Math.Max(rounded, fallback);
    }

    public static IEnumerable<string> Split(string? colors) =>
        (colors ?? string.Empty)
            .Split([',', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Canonical);
}
