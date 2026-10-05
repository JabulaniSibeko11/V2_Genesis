using System.Globalization;

namespace V2_Genesis.Helpers;

/// <summary>
/// Writes a roll "With Effective Date" (WefDate) in full, e.g.
/// "10/01/2025 00:00:00" → "10 January 2025".
/// Roll dates are day-first (South African format), so 10/01/2025 is
/// 10 January, never 1 October. Returns "" for an empty / NULL value and
/// the original text if it is not a date.
/// </summary>
public static class WefDateFormatter
{
    private static readonly CultureInfo ZA = CultureInfo.GetCultureInfo("en-ZA");

    private static readonly string[] Formats =
    {
        "dd/MM/yyyy HH:mm:ss", "d/M/yyyy HH:mm:ss", "dd/MM/yyyy H:mm:ss",
        "dd/MM/yyyy HH:mm", "d/M/yyyy HH:mm",
        "dd/MM/yyyy", "d/M/yyyy",
        "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy",
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-dd", "yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd",
        "yyyyMMdd"
    };

    public static string Format(string? value)
    {
        var raw = value?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (DateTime.TryParseExact(raw, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d) ||
            DateTime.TryParse(raw, ZA, DateTimeStyles.AllowWhiteSpaces, out d))
        {
            return d.ToString("dd MMMM yyyy", ZA);
        }

        return raw;
    }

    public static string Format(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd MMMM yyyy", ZA) : string.Empty;
}
