namespace V2_Genesis.Services.Objection;

/// <summary>
/// OBJECTION PACK and APPEAL PACK — one folder per reference number.
///
///   Objection Pack   ObjectionRolls:{roll}:FileRootPath\{Objection_No}\
///                    e.g. C:\Sup4\Sup 4 Objections\Sup4Data\GV23-Sup4-24
///   Appeal Pack      ObjectionRolls:{roll}:AppealRootPath\{Appeal_No}\
///                    e.g. C:\Sup4\Sup 4 Objections\Sup4AppealData\APP-GV23-Sup4-7
///
/// What Genesis saves in the pack:
///
///   Objection Pack                        Appeal Pack
///   ──────────────                        ───────────
///   Acknowledgement of Objection (PDF)    {Objection_No} Objection Pack.zip
///   E-mail copy of the acknowledgement    Acknowledgement of Appeal (PDF)
///   Objection Form (PDF)                  E-mail copy of the acknowledgement
///   Section 49 Notice (PDF, owner)        Appeal Form (PDF)
///   Representative\                       Representative\
///   Submitted Evidence\                   Submitted Evidence\
///   Section 51 Notice\  (PDF + e-mail, Third-Party only)
///   Section 51 Owner Evidence\
///
/// The Section 53 MVD and the appeal decision are added by the other
/// systems. Sub-folders are only created when there is something to save
/// (e.g. "Representative" only when a representative lodged).
///
/// The folder names can be changed in appsettings.json → "PackFolders".
/// </summary>
public static class PackFolders
{
    public static string Representative(IConfiguration config) =>
        Name(config, "Representative", "Representative");

    public static string SubmittedEvidence(IConfiguration config) =>
        Name(config, "SubmittedEvidence", "Submitted Evidence");

    public static string Section51Notice(IConfiguration config) =>
        Name(config, "Section51Notice", "Section 51 Notice");

    public static string Section51OwnerEvidence(IConfiguration config) =>
        Name(config, "Section51OwnerEvidence", "Section 51 Owner Evidence");

    /// File name of the zipped Objection Pack inside an Appeal Pack.
    public static string ObjectionPackZipName(string objectionNo) =>
        $"{SafeName(objectionNo)} Objection Pack.zip";

    private static string Name(IConfiguration config, string key, string fallback)
    {
        var value = config[$"PackFolders:{key}"];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    public static string SafeName(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            text = text.Replace(c, '_');
        return text;
    }
}
