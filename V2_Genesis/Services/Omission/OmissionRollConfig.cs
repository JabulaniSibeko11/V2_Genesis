namespace V2_Genesis.Services.Omission
{
    public record OmissionRollConfig(
     string ConnectionKey,
     string TownSp,     // propertyDetailsTown / propertyDetailsTown_Sup1
     string SchemeSp    // propertyDetailsScheme / propertyDetailsScheme_Sup1
 );

    public static class OmissionRollRegistry
    {
        public static IReadOnlyDictionary<string, OmissionRollConfig> Build()
        {
            static OmissionRollConfig Make(string connKey, string suffix) => new(
                ConnectionKey: connKey,
                TownSp: $"propertyDetailsTown{suffix}",
                SchemeSp: $"propertyDetailsScheme{suffix}"
            );

            // Each roll DB has the same SP names — no suffix needed.
            // The correct DB is selected via the ConnectionKey.
            return new Dictionary<string, OmissionRollConfig>
            {
                ["Objection"] = Make("DefaultConnection", ""),
                ["Objection_Supp1"] = Make("Sup1Connection", ""),
                ["Objection_Supp2"] = Make("Sup2Connection", ""),
                ["Objection_Supp3"] = Make("Sup3Connection", ""),
                ["Objection_Supp4"] = Make("Sup4Connection", ""),
                ["Objection_Supp5"] = Make("Sup5Connection", ""),
            };
        }
    }
}