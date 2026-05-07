namespace V2_Genesis.Services.Objection;

public class ObjectionRollEntry
{
    public string FileRootPath { get; set; } = string.Empty;
    public string ObjPrefix { get; set; } = string.Empty;
    public string AppealPrefix { get; set; } = string.Empty;
}

public class ObjectionRollSettings
{
    public Dictionary<string, ObjectionRollEntry> ObjectionRolls { get; set; } = new();

    public ObjectionRollEntry For(string rollSource) =>
        ObjectionRolls.TryGetValue(rollSource, out var entry)
            ? entry
            : new ObjectionRollEntry
            {
                FileRootPath = "C:\\ObjectionFiles\\Default",
                ObjPrefix = rollSource,
                AppealPrefix = "APP-" + rollSource
            };
}