namespace V2_Genesis.Services.Notice;

/// <summary>
/// One place that knows where notice files live on disk.
///
/// appsettings.json stores the folders as
///   ObjectionRolls:{roll}:FileRootPath      (objection folders)
///   ObjectionRolls:{roll}:AppealRootPath    (appeal folders)
///   ObjectionRolls:Objection_Query:QueryRootPath
///   ObjectionRolls:Section49:Section49RootPath
///   ObjectionRolls:Rebates:RebateRooTPath
///   Section51Rolls:{roll}:FileRootPath
///
/// The notice list and the download action used to read different keys
/// ("...:RootPath", "AppSettings:Section49RootPath",
/// "AppSettings:AppealRootPath") that are not in appsettings.json, so every
/// root came back empty: no notice was ever found and every download was
/// refused. The old keys are still honoured first, so an existing server
/// config keeps working.
/// </summary>
public static class NoticeStoragePaths
{
    public static readonly string[] ObjectionRolls =
    {
        "Objection",
        "Objection_Supp1",
        "Objection_Supp2",
        "Objection_Supp3",
        "Objection_Supp4",
        "Objection_Supp5"
    };

    private static string First(IConfiguration config, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = config[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    /// Objection folder root for a roll (Objection, Objection_Supp1, ...).
    public static string ObjectionRoot(IConfiguration config, string roll) =>
        First(config,
            $"ObjectionRolls:{roll}:RootPath",
            $"ObjectionRolls:{roll}:FileRootPath");

    /// Appeal folder root for a roll. Falls back to the old single
    /// AppSettings:AppealRootPath.
    public static string AppealRoot(IConfiguration config, string roll) =>
        First(config,
            $"ObjectionRolls:{roll}:AppealRootPath",
            "AppSettings:AppealRootPath");

    public static string Section49Root(IConfiguration config) =>
        First(config,
            "AppSettings:Section49RootPath",
            "ObjectionRolls:Section49:Section49RootPath");

    public static string QueryRoot(IConfiguration config) =>
        First(config,
            "ObjectionRolls:Objection_Query:QueryRootPath");

    public static string RebateRoot(IConfiguration config) =>
        First(config,
            "ObjectionRolls:Rebates:RebateRooTPath",
            "ObjectionRolls:Rebates:RebateRootPath");

    /// Every folder a notice may be downloaded from (used as the
    /// path-traversal safe list).
    public static IReadOnlyList<string> AllRoots(IConfiguration config)
    {
        var roots = new List<string>();

        foreach (var roll in ObjectionRolls)
        {
            roots.Add(ObjectionRoot(config, roll));
            roots.Add(AppealRoot(config, roll));
            roots.Add(First(config, $"Section51Rolls:{roll}:FileRootPath"));
        }

        roots.Add(First(config, "AppSettings:AppealRootPath"));
        roots.Add(Section49Root(config));
        roots.Add(QueryRoot(config));
        roots.Add(RebateRoot(config));

        return roots
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
