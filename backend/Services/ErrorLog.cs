using System.Text.Json;
using System.Text.RegularExpressions;

namespace backend.Services;

public sealed record ErrorLogEntry(
    string Username,
    DateTimeOffset OccurredAt,
    string Date,
    string Time,
    string Message,
    string Detail,
    string Source,
    string Path,
    string Area);

public interface IErrorLog
{
    void Write(string? username, string message, string? detail, string path, string area);
    IReadOnlyList<ErrorLogEntry> ReadRecent(int take = 200);
    string FilePath { get; }
}

public sealed class ErrorLog(IConfiguration configuration) : IErrorLog
{
    private static readonly object Gate = new();
    private readonly string directory = ErrorLogPath.Resolve(configuration["ErrorLog:Directory"]);
    private readonly string fileName = string.IsNullOrWhiteSpace(configuration["ErrorLog:FileName"])
        ? "ubhaya-errors.log"
        : configuration["ErrorLog:FileName"]!;

    public string FilePath => Path.Combine(directory, fileName);

    public void Write(string? username, string message, string? detail, string path, string area)
    {
        var occurred = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, IndiaTime());
        var entry = new ErrorLogEntry(
            string.IsNullOrWhiteSpace(username) ? "Anonymous" : username.Trim(),
            occurred,
            occurred.ToString("dd MMM yyyy"),
            occurred.ToString("hh:mm:ss tt"),
            Redact(message),
            Redact(detail ?? ""),
            "backend",
            path,
            area);

        try
        {
            Directory.CreateDirectory(directory);
            var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
            lock (Gate)
            {
                using var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.Write(line);
            }
        }
        catch
        {
            // Logging must not replace the original error.
        }
    }

    public IReadOnlyList<ErrorLogEntry> ReadRecent(int take = 200)
    {
        if (!File.Exists(FilePath)) return [];

        string[] lines;
        lock (Gate)
        {
            lines = File.ReadAllLines(FilePath);
        }

        return lines
            .Reverse()
            .Take(Math.Clamp(take, 1, 500))
            .Select(line =>
            {
                try { return JsonSerializer.Deserialize<ErrorLogEntry>(line); }
                catch (JsonException) { return null; }
            })
            .Where(entry => entry is not null)
            .Cast<ErrorLogEntry>()
            .ToList();
    }

    private static TimeZoneInfo IndiaTime()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Local; }
    }

    internal static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var text = value.Length > 8000 ? value[..8000] : value;
        return Regex.Replace(
            text,
            "(key_secret|secret|password|cvv|cardnumber|razorpay_signature)(\"?\\s*[:=]\\s*\"?)[^\\s,\"']+",
            "$1$2[redacted]",
            RegexOptions.IgnoreCase);
    }
}

public static class ErrorLogPath
{
    public static string Resolve(string? configured)
    {
        var requested = string.IsNullOrWhiteSpace(configured) ? "c:/logs" : configured.Trim();
        if (OperatingSystem.IsLinux() && requested.StartsWith("c:", StringComparison.OrdinalIgnoreCase))
        {
            var rest = requested[2..].Replace('\\', '/').TrimStart('/');
            return Path.Combine("/mnt/c", rest);
        }

        return requested;
    }
}
