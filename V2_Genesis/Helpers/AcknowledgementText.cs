namespace V2_Genesis.Helpers;

/// <summary>
/// Headings of the objection / appeal acknowledgement PDF
/// (NoticeService.BuildAcknowledgementPdf). Unit-tested in V2_Genesis.Tests.
/// </summary>
public static class AcknowledgementText
{
    /// Appeal: "{ROLL} APPEAL ACKNOWLEDGEMENT"; objection unchanged.
    public static string Title(bool isAppeal, bool isMulti, string rollTitle)
    {
        var roll = (rollTitle ?? string.Empty).Trim().ToUpperInvariant();

        if (isAppeal)
        {
            var label = isMulti ? "MULTIPURPOSE APPEAL ACKNOWLEDGEMENT" : "APPEAL ACKNOWLEDGEMENT";
            return string.IsNullOrWhiteSpace(roll) ? label : $"{roll} {label}";
        }

        return isMulti ? "MULTIPURPOSE OBJECTION ACKNOWLEDGEMENT" : "OBJECTION ACKNOWLEDGEMENT";
    }

    /// Appeal: details "AS LISTED IN MUNICIPAL VALUER DECISION" (Section 6).
    public static string ListedTitle(bool isAppeal, bool isLis, string rollTitle) =>
        isAppeal
            ? "PROPERTY DETAILS AS LISTED IN MUNICIPAL VALUER DECISION"
            : isLis
                ? "PROPERTY DETAILS AS LISTED IN LIS"
                : $"PROPERTY DETAILS AS LISTED IN {(rollTitle ?? string.Empty).Trim().ToUpperInvariant()}";

    /// Appeal period line from Objection_MVD (start 08:00 – close 15:00).
    /// No close date → only the roll name.
    public static string AppealPeriod(string rollTitle, DateTime? start, DateTime? close)
    {
        var roll = (rollTitle ?? string.Empty).Trim().ToUpperInvariant();

        if (!close.HasValue)
            return roll;

        var from = start.HasValue ? $"{start.Value:dd MMMM yyyy} AT 08:00 - " : string.Empty;
        return $"{roll}\nAPPEAL PERIOD: {from}{close.Value:dd MMMM yyyy} AT 15:00";
    }
}
