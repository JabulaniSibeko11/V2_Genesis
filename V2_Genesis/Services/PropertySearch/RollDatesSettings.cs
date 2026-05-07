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
    // Must match the appsettings key exactly — no nested "Dates" wrapper
    public Dictionary<string, RollDateEntry> Dates { get; set; } = new();

    public RollDateEntry? For(string rollSource) =>
        Dates.TryGetValue(rollSource, out var entry) ? entry : null;
}