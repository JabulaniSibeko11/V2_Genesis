namespace V2_Genesis.Services.Lis;

public record LisRollConfig(
    string ConnectionKey,
    string TownOnly,
    string TownStand,
    string TownStandAddress,
    string TownScheme,
    string TownAddress,
    string TownUnit,
    string SchemeUnit,
    string TownErfScheme,
    string TownAddressScheme,
    string TownAndScheme,
    string DetailSp
);

public static class LisRollRegistry
{
    public static IReadOnlyDictionary<string, LisRollConfig> Build()
    {
        static LisRollConfig Make(string connKey) => new(
            ConnectionKey: connKey,

            // IMPORTANT:
            // Stored procedure names do NOT use Sup suffixes.
            TownOnly: "SearchTownLIS",
            TownStand: "SearchTownStandNumberLIS",
            TownStandAddress: "StandTownStandNumberAddressLIS",
            TownScheme: "SearchTownSchemeLIS",
            TownAddress: "SearchTownAddressLIS",
            TownUnit: "SearchTownUnitLIS",
            SchemeUnit: "SearchTownSchemeUnitLIS",
            TownErfScheme: "SearchTownERFSchemeLIS",
            TownAddressScheme: "SearchTownAddressSchemeLIS",
            TownAndScheme: "GetDistinctTownAndScheme",
            DetailSp: "IndexObjectionLIS"
        );

        return new Dictionary<string, LisRollConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["Objection"] = Make("DefaultConnection"),
            ["Objection_Supp1"] = Make("Sup1Connection"),
            ["Objection_Supp2"] = Make("Sup2Connection"),
            ["Objection_Supp3"] = Make("Sup3Connection"),
            ["Objection_Supp4"] = Make("Sup4Connection"),
            ["Objection_Supp5"] = Make("Sup5Connection"),
        };
    }
}