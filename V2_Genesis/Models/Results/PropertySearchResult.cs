namespace V2_Genesis.Models;

/// <summary>
/// Unified property search result — same columns returned by all roll SPs.
/// </summary>
public class PropertySearchResult
{
    public int Id { get; set; }
    public string? TownNameDesc { get; set; }
    public int Erf { get; set; }
    public int Ptn { get; set; }
    public string? Re { get; set; }
    public string? LisStreetAddress { get; set; }
    public string? SchemeName { get; set; }
    public string? SchemeNumber { get; set; }
    public int UnitNo { get; set; }
    public string? SchemeYear { get; set; }
    public string? UnitKey { get; set; }
    public string? MarketValue { get; set; }
    public string? CatDesc { get; set; }
    public string? RateableArea { get; set; }
    public string? WefDate { get; set; }
    public string? ValuationDate { get; set; }
    public string? ValuationKey { get; set; }
    public string? UnitType { get; set; }
    public string? Reason { get; set; }
    public string? AdditionalNotes { get; set; }
    public string? Lease { get; set; }

    // ── Computed display helpers ──────────────────────────────────────
    public string PropertyDisplay =>
        !string.IsNullOrWhiteSpace(SchemeName)
            ? $"{SchemeName} Unit {UnitNo} – {TownNameDesc}"
            : $"Erf {Erf} Ptn {Ptn}{(string.IsNullOrWhiteSpace(Re) ? "" : $" RE {Re}")} – {TownNameDesc}";

    public string AddressDisplay =>
        LisStreetAddress ?? "–";
}