namespace V2_Genesis.Services.Section51;

/// <summary>
/// appsettings.json → Section51Rolls:{roll}.
///
///   PostalAddressTable  Owner postal/email table used for the Section 51
///                       notice sent when a Third-Party objection is
///                       submitted (ADDR1–ADDR5, EMAIL_ADDR, PREMISE_ID).
///   RollId              Value written to Section51Table.RollId.
///   RollName            Roll name printed on the notice and in the email.
/// </summary>
public record Section51RollConfig(
    string ValidateSp,
    string CheckSp,
    string FileRootPath,
    DateTime DeadlineUtc,
    string ConnectionKey,
    string PostalAddressTable,
    string RollId,
    string RollName
);

public static class Section51RollRegistry
{
    private static readonly Dictionary<string, (string Table, string RollId, string RollName)> Defaults =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Objection"] = ("Objection_Postal_address", "1", "General Valuation Roll 2023"),
            ["Objection_Supp1"] = ("Supp1_Postal_address", "2", "Supplementary Valuation Roll 1"),
            ["Objection_Supp2"] = ("Supp2_Postal_address", "3", "Supplementary Valuation Roll 2"),
            ["Objection_Supp3"] = ("Supp3_Postal_address", "4", "Supplementary Valuation Roll 3"),
            ["Objection_Supp4"] = ("Supp4_Postal_address", "5", "Supplementary Valuation Roll 4"),
            ["Objection_Supp5"] = ("Supp5_Postal_address", "6", "Supplementary Valuation Roll 5"),
        };

    public static IReadOnlyDictionary<string, Section51RollConfig> Build(
        IConfiguration config)
    {
        Section51RollConfig Load(string key)
        {
            var defaults = Defaults[key];

            string Value(string name, string fallback)
            {
                var value = config[$"Section51Rolls:{key}:{name}"];
                return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            }

            return new Section51RollConfig(
                ValidateSp: config[$"Section51Rolls:{key}:ValidateSp"] ?? "Section51",
                CheckSp: config[$"Section51Rolls:{key}:CheckSp"] ?? "Section51Check",
                FileRootPath: config[$"Section51Rolls:{key}:FileRootPath"] ?? string.Empty,
                DeadlineUtc: DateTime.TryParse(
                                   config[$"Section51Rolls:{key}:DeadlineUtc"],
                                   out var d) ? d : DateTime.MaxValue,
                ConnectionKey: config[$"Section51Rolls:{key}:ConnectionKey"] ?? "Sup3Connection",
                PostalAddressTable: Value("PostalAddressTable", defaults.Table),
                RollId: Value("RollId", defaults.RollId),
                RollName: Value("RollName", defaults.RollName)
            );
        }

        return new Dictionary<string, Section51RollConfig>
        {
            ["Objection"] = Load("Objection"),
            ["Objection_Supp1"] = Load("Objection_Supp1"),
            ["Objection_Supp2"] = Load("Objection_Supp2"),
            ["Objection_Supp3"] = Load("Objection_Supp3"),
            ["Objection_Supp4"] = Load("Objection_Supp4"),
            ["Objection_Supp5"] = Load("Objection_Supp5"),
        };
    }
}
