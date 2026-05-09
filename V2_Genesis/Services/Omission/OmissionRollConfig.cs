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

            return new Dictionary<string, OmissionRollConfig>
            {
                ["Objection"] = Make("DefaultConnection", ""),
                ["Objection_Supp1"] = Make("Sup1Connection", "_Sup1"),
                ["Objection_Supp2"] = Make("Sup2Connection", "_Sup2"),
                ["Objection_Supp3"] = Make("Sup3Connection", "_Sup3"),
            };
        }
    }
}
