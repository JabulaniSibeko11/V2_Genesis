namespace V2_Genesis.Services.Objection;

/// <summary>
/// Appeal period of an objection, from Objection_MVD
/// (Appeal_Start_Date / Appeal_Close_Date, or the ReviseMVD dates when the
/// MVD was revised). Same rule for clients and for the admin team:
/// an appeal can only be lodged inside the period (no late appeals).
/// Pure code so it can be unit-tested (V2_Genesis.Tests/Services/AppealWindowRulesTests).
/// </summary>
public static class AppealWindowRules
{
    public const string Open = "Open";
    public const string NotYetOpen = "NotYetOpen";
    public const string Closed = "Closed";
    public const string Unknown = "Unknown";

    public sealed record Window(DateTime? Start, DateTime? Close, bool Revised);

    /// The revised dates win when the MVD was revised; a missing revised date
    /// falls back to the original one.
    public static Window Resolve(
        DateTime? start,
        DateTime? close,
        DateTime? revisedStart,
        DateTime? revisedClose,
        string? reviseMvd)
    {
        var flag = (reviseMvd ?? string.Empty).Trim();
        var revised =
            flag.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            flag.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
            flag.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
            flag == "1" ||
            revisedClose.HasValue;

        return revised
            ? new Window(revisedStart ?? start, revisedClose ?? close, true)
            : new Window(start, close, false);
    }

    /// Open / NotYetOpen / Closed / Unknown (no dates). Dates are inclusive.
    public static string State(DateTime? start, DateTime? close, DateTime today)
    {
        if (!close.HasValue)
            return Unknown;

        if (start.HasValue && today.Date < start.Value.Date)
            return NotYetOpen;

        return today.Date <= close.Value.Date ? Open : Closed;
    }

    /// South African date (the appeal dates are SA dates).
    public static DateTime TodaySa()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        }
        catch
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            }
            catch
            {
                return DateTime.UtcNow.AddHours(2).Date;
            }
        }
    }
}
