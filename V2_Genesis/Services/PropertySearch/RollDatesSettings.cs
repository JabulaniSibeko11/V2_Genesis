namespace V2_Genesis.Services.PropertySearch;

/// <summary>
/// One entry per roll — bound from appsettings "RollDates" section.
/// Key = GvList.Source value (e.g. "Objection_Supp3").
/// </summary>
public class RollDateEntry
{
    public DateTime OpenDate { get; set; }
    public DateTime VisibleUntil { get; set; }
}

public class RollDatesSettings
{
    /// <summary>Keyed by GvList.Source.</summary>
    public Dictionary<string, RollDateEntry> Dates { get; set; } = new();

    /// <summary>
    /// Returns the entry for the given roll source,
    /// or a safe default (both dates in the past) if not found.
    /// </summary>
    public RollDateEntry For(string rollSource) =>
        Dates.TryGetValue(rollSource, out var entry)
            ? entry
            : new RollDateEntry
            {
                OpenDate = DateTime.MinValue,
                VisibleUntil = DateTime.MinValue
            };
}