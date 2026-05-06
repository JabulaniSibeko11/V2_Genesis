namespace GenesisV2.Services.PropertySearch;

public record RollSearchConfig(
    string SpTown,
    string SpStand,
    string SpStandAddress,
    string SpAddress,
    string SpScheme,
    string SpUnit,
    string SpSchemeUnit,
    string SpStandScheme,
    string SpAddressScheme,
    string DetailSp,
    string ConnectionKey   // ← used for BOTH search SPs and detail SP
);

public static class RollSearchRegistry
{
    public static readonly IReadOnlyDictionary<string, RollSearchConfig> Configs =
        new Dictionary<string, RollSearchConfig>
        {
            // ── GV Roll — SPs use explicit Objection.dbo. prefix → DefaultConnection
            ["Objection"] = new(
                SpTown: "Objection.dbo.SearchTown",
                SpStand: "Objection.dbo.SearchTownStandNumber",
                SpStandAddress: "Objection.dbo.SearchTownStandNumberAddress",
                SpAddress: "Objection.dbo.SearchTownAddress",
                SpScheme: "Objection.dbo.SearchTownScheme",
                SpUnit: "Objection.dbo.SearchTownUnit",
                SpSchemeUnit: "Objection.dbo.SearchTownSchemeUnit",
                SpStandScheme: "Objection.dbo.SearchTownErfScheme",
                SpAddressScheme: "Objection.dbo.SearchTownAddressScheme",
                DetailSp: "IndexObjection",
                ConnectionKey: "DefaultConnection"
            ),

            // ── Sup1 — SPs live in Objection_Supp1 database
            ["Objection_Supp1"] = new(
                SpTown: "SearchTown_Sup1",
                SpStand: "SearchTownStandNumber_Sup1",
                SpStandAddress: "StandTownStandNumberAddress_Sup1",
                SpAddress: "SearchTownAddress_Sup1",
                SpScheme: "SearchTownScheme_Sup1",
                SpUnit: "SearchTownUnit_Sup1",
                SpSchemeUnit: "SearchTownSchemeUnit_Sup1",
                SpStandScheme: "SearchTownERFScheme_Sup1",
                SpAddressScheme: "SearchTownAddressScheme_Sup1",
                DetailSp: "IndexObjection_Sup1",
                ConnectionKey: "Sup1Connection"
            ),

            // ── Sup2 — SPs live in Objection_Supp2 database
            ["Objection_Supp2"] = new(
                SpTown: "SearchTown_Sup2",
                SpStand: "SearchTownStandNumber",
                SpStandAddress: "StandTownStandNumberAddress",
                SpAddress: "SearchTownAddress_Sup2",
                SpScheme: "SearchTownScheme_Sup2",
                SpUnit: "SearchTownUnit_Sup2",
                SpSchemeUnit: "SearchTownSchemeUnit_Sup2",
                SpStandScheme: "SearchTownERFScheme_Sup2",
                SpAddressScheme: "SearchTownAddressScheme_Sup2",
                DetailSp: "IndexObjection_Sup2",
                ConnectionKey: "Sup2Connection"
            ),

            // ── Sup3 — SPs live in Objection_Supp3 database
            ["Objection_Supp3"] = new(
                SpTown: "SearchTown",
                SpStand: "SearchTownStandNumber",
                SpStandAddress: "StandTownStandNumberAddress",
                SpAddress: "SearchTownAddress",
                SpScheme: "SearchTownScheme",
                SpUnit: "SearchTownUnit",
                SpSchemeUnit: "SearchTownSchemeUnit",
                SpStandScheme: "SearchTownERFScheme",
                SpAddressScheme: "SearchTownAddressScheme",
                DetailSp: "IndexObjection_Sup3",
                ConnectionKey: "Sup3Connection"
            ),
        };
}