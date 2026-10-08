using QuestPDF.Drawing;

namespace V2_Genesis.Helpers;

/// <summary>
/// QuestPDF 2026.9+ no longer uses the server's installed (system) fonts by
/// default, so every PDF that asks for FontFamily("Arial") failed with
/// "font families that are not available: 'Arial'".
///
/// Called once at start-up (Program.cs):
///   1. turns system fonts back on (Arial is installed on the Windows / IIS server);
///   2. also registers Arial directly from the Windows fonts folder and from an
///      optional "Fonts" folder next to the app (drop arial*.ttf there if a
///      server ever has no Arial). Fonts are not stored in git.
/// </summary>
public static class QuestPdfFonts
{
    private static readonly string[] ArialFiles =
    {
        "arial.ttf", "arialbd.ttf", "ariali.ttf", "arialbi.ttf"
    };

    public static void Configure()
    {
        QuestPDF.Settings.UseSystemFonts = true;

        var folders = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Fonts"),
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts)
        };

        foreach (var folder in folders)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                continue;

            foreach (var name in ArialFiles)
            {
                var path = Path.Combine(folder, name);
                if (!File.Exists(path))
                    continue;

                try
                {
                    using var stream = File.OpenRead(path);
                    FontManager.RegisterFont(stream);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "QuestPDF: could not register font {FontPath}", path);
                }
            }
        }
    }
}
