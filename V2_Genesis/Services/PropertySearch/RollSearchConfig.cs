namespace V2_Genesis.Services.PropertySearch;

/// <summary>
/// Stored procedure names for one roll.
/// All SPs receive wildcarded params: @SearchTownName = '%value%'
/// Verify Sup1 SP names against your database before deploying.
/// </summary>
public record RollSearchConfig(
    string SpTown,
    string SpStand,
    string SpStandAddress,
    string SpAddress,
    string SpScheme,
    string SpUnit,
    string SpSchemeUnit,
    string SpStandScheme,
    string SpAddressScheme
);

public static class RollSearchRegistry
{
    /// <summary>Keyed by GvList.Source value.</summary>
    public static readonly IReadOnlyDictionary<string, RollSearchConfig> Configs =
        new Dictionary<string, RollSearchConfig>
        {
            // ── GV23 General Valuation Roll ───────────────────────────────
            ["Objection"] = new(
                SpTown: "Objection.dbo.SearchTown",
                SpStand: "Objection.dbo.SearchTownStandNumber",
                SpStandAddress: "Objection.dbo.SearchTownStandNumberAddress",
                SpAddress: "Objection.dbo.SearchTownAddress",
                SpScheme: "Objection.dbo.SearchTownScheme",
                SpUnit: "Objection.dbo.SearchTownUnit",
                SpSchemeUnit: "Objection.dbo.SearchTownSchemeUnit",
                SpStandScheme: "Objection.dbo.SearchTownErfScheme",
                SpAddressScheme: "Objection.dbo.SearchTownAddressScheme"
            ),

            // ── Supplementary Roll 1 — TODO: verify SP names ──────────────
            ["Objection_Supp1"] = new(
                SpTown: "SearchTown_Sup1",
                SpStand: "SearchTownStandNumber_Sup1",
                SpStandAddress: "StandTownStandNumberAddress_Sup1",
                SpAddress: "SearchTownAddress_Sup1",
                SpScheme: "SearchTownScheme_Sup1",
                SpUnit: "SearchTownUnit_Sup1",
                SpSchemeUnit: "SearchTownSchemeUnit_Sup1",
                SpStandScheme: "SearchTownERFScheme_Sup1",
                SpAddressScheme: "SearchTownAddressScheme_Sup1"
            ),

            // ── Supplementary Roll 2 ──────────────────────────────────────
            ["Objection_Supp2"] = new(
                SpTown: "SearchTown_Sup2",
                SpStand: "SearchTownStandNumber",
                SpStandAddress: "StandTownStandNumberAddress",
                SpAddress: "SearchTownAddress_Sup2",
                SpScheme: "SearchTownScheme_Sup2",
                SpUnit: "SearchTownUnit_Sup2",
                SpSchemeUnit: "SearchTownSchemeUnit_Sup2",
                SpStandScheme: "SearchTownERFScheme_Sup2",
                SpAddressScheme: "SearchTownAddressScheme_Sup2"
            ),

            // ── Supplementary Roll 3 ──────────────────────────────────────
            ["Objection_Supp3"] = new(
                SpTown: "SearchTown",
                SpStand: "SearchTownStandNumber",
                SpStandAddress: "StandTownStandNumberAddress",
                SpAddress: "SearchTownAddress",
                SpScheme: "SearchTownScheme",
                SpUnit: "SearchTownUnit",
                SpSchemeUnit: "SearchTownSchemeUnit",
                SpStandScheme: "SearchTownERFScheme",
                SpAddressScheme: "SearchTownAddressScheme"
            ),
        };
}