using System.Globalization;

namespace V2_Genesis.Helpers;

/// <summary>
/// Number formats of the notices (same as eNotice):
///   Rand   "R 29 184 000"
///   Extent "1 174" (whole) or "1 174.50"
/// Unit-tested in V2_Genesis.Tests.
/// </summary>
public static class NoticeNumberFormat
{
    public static string Rand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var raw = value.Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty).Trim();
        raw = new string(raw.Where(ch => char.IsDigit(ch) || ch == '.' || ch == '-').ToArray());

        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
            ? "R " + amount.ToString("#,##0", CultureInfo.InvariantCulture).Replace(",", " ")
            : value.Trim();
    }

    public static string Extent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var raw = value.Replace(",", string.Empty).Trim();
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var extent))
            return value.Trim();

        return extent == Math.Truncate(extent)
            ? extent.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ")
            : extent.ToString("N2", CultureInfo.InvariantCulture).Replace(",", " ");
    }
}
