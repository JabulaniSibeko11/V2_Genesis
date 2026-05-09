
namespace V2_Genesis.Services.Lis;


public record LisRollConfig(
    string ConnectionKey,
    string TownOnly,               // SearchTownLIS
    string TownStand,              // SearchTownStandNumberLIS
    string TownStandAddress,       // StandTownStandNumberAddressLIS
    string TownScheme,             // SearchTownSchemeLIS
    string TownAddress,            // SearchTownAddressLIS
    string TownUnit,               // SearchTownUnitLIS
    string SchemeUnit,             // SearchTownSchemeUnitLIS
    string TownErfScheme,          // SearchTownERFSchemeLIS
    string TownAddressScheme,      // SearchTownAddressSchemeLIS
    string TownAndScheme           // GetDistinctTownAndScheme (dropdown)
);

public static class LisRollRegistry
{
    public static IReadOnlyDictionary<string, LisRollConfig> Build()
    {
        static LisRollConfig Make(string connKey, string suffix) => new(
            ConnectionKey: connKey,
            TownOnly: $"SearchTownLIS{suffix}",
            TownStand: $"SearchTownStandNumberLIS{suffix}",
            TownStandAddress: $"StandTownStandNumberAddressLIS{suffix}",
            TownScheme: $"SearchTownSchemeLIS{suffix}",
            TownAddress: $"SearchTownAddressLIS{suffix}",
            TownUnit: $"SearchTownUnitLIS{suffix}",
            SchemeUnit: $"SearchTownSchemeUnitLIS{suffix}",
            TownErfScheme: $"SearchTownERFSchemeLIS{suffix}",
            TownAddressScheme: $"SearchTownAddressSchemeLIS{suffix}",
            TownAndScheme: $"GetDistinctTownAndScheme{suffix}"
        );

        return new Dictionary<string, LisRollConfig>
        {
            ["Objection"] = Make("DefaultConnection", ""),
            ["Objection_Supp1"] = Make("Sup1Connection", "_Sup1"),
            ["Objection_Supp2"] = Make("Sup2Connection", "_Sup2"),
            ["Objection_Supp3"] = Make("Sup3Connection", "_Sup3"),
        };
    }
}