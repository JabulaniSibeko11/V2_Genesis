namespace V2_Genesis.Services.PropertySearch;

/// <summary>
/// One entry per roll — bound from appsettings "RollDates" section.
/// Key = GvList.Source value (e.g. "Objection_Supp3").
/// </summary>
public class RollDateEntry
{
    public DateTime OpenDate { get; set; }
    public DateTime VisibleUntil { get; set; }
    public string? ExtendedPeriodText { get; set; }  
}

public class RollDatesSettings
{
    // Must match the appsettings key exactly — no nested "Dates" wrapper
    public Dictionary<string, RollDateEntry> Dates { get; set; } = new();

    public RollDateEntry? For(string rollSource) =>
        Dates.TryGetValue(rollSource, out var entry) ? entry : null;

    /// The current roll = the roll with the latest OpenDate (today
    /// Objection_Supp4). When Supp5 is added to RollDates it takes over.
    public static string? CurrentRollSource(IDictionary<string, RollDateEntry> dates) =>
        dates
            .OrderByDescending(x => x.Value.OpenDate)
            .Select(x => x.Key)
            .FirstOrDefault();

    public string? CurrentRollSource() => CurrentRollSource(Dates);

    /// Property SEARCH / VIEW rule (linking is stricter — see
    /// CanSearchAndLinkRoll): not before the roll opens; during the
    /// objection period; and after it closes only for the current roll,
    /// so clients can keep viewing properties until the next roll opens.
    public static bool CanSearch(
        IDictionary<string, RollDateEntry> dates,
        string rollSource,
        DateTime now)
    {
        if (!dates.TryGetValue(rollSource, out var entry))
            return true;

        if (now < entry.OpenDate)
            return false;

        if (now <= entry.VisibleUntil)
            return true;

        return string.Equals(
            rollSource,
            CurrentRollSource(dates),
            StringComparison.OrdinalIgnoreCase);
    }

    public bool CanSearch(string rollSource) =>
        CanSearch(Dates, rollSource, DateTime.Now);
}