namespace V2_Genesis.Services.Section51;

public record Section51RollConfig(
    string ValidateSp,
    string CheckSp,
    string FileRootPath,
    DateTime DeadlineUtc,
    string ConnectionKey
);

public static class Section51RollRegistry
{
    public static IReadOnlyDictionary<string, Section51RollConfig> Build(
        IConfiguration config)
    {
        Section51RollConfig Load(string key) => new(
            ValidateSp: config[$"Section51Rolls:{key}:ValidateSp"] ?? "Section51",
            CheckSp: config[$"Section51Rolls:{key}:CheckSp"] ?? "Section51Check",
            FileRootPath: config[$"Section51Rolls:{key}:FileRootPath"] ?? string.Empty,
            DeadlineUtc: DateTime.TryParse(
                               config[$"Section51Rolls:{key}:DeadlineUtc"],
                               out var d) ? d : DateTime.MaxValue,
            ConnectionKey: config[$"Section51Rolls:{key}:ConnectionKey"] ?? "Sup3Connection"
        );

        return new Dictionary<string, Section51RollConfig>
        {
            ["Objection"] = Load("Objection"),
            ["Objection_Supp1"] = Load("Objection_Supp1"),
            ["Objection_Supp2"] = Load("Objection_Supp2"),
            ["Objection_Supp3"] = Load("Objection_Supp3"),
        };
    }
}